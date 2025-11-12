using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChainSim : MonoBehaviour
{
    [Header("Chain Settings")]
    public int N = 20;
    public float nodeMass = 0.08f;
    public float restLength = 0.15f;
    public float k = 600f;
    public float c = 0.1f;
    public float airFriction = 0.004f;
    public float breakForce = 80f;
    [Range(0, 2)] public float alpha = 1.0f;

    [Header("Integration")]
    public float substepDt = 1f / 600f;
    public int substepsPerFrame = 8;
    public int constraintIters = 6;
    public Vector2 gravity = new Vector2(0, -9.81f);

    [Header("Anchors")]
    public Transform topAnchor;
    public Transform bottomAnchor;

    [Header("Visuals")]
    public GameObject linkPrefab;
    public Color lowStressColor = Color.white;
    public Color highStressColor = Color.red;

    [Header("Release Control")]
    public float releaseTime = 1.5f;
    public float kickMultiplier = 3.0f;   // 🔥 new: multiplies impulse strength
    bool released = false;
    bool hasDropped = false;
    bool fractured = false;

    struct Node { public Vector2 x, v; public float m; public bool pinned; }
    struct Link { public int i, j; public float L0, k, c; public bool broken; }

    Node[] nodes;
    List<Link> links = new();
    List<GameObject> visuals = new();

    void Start()
    {
        nodes = new Node[N];
        Vector2 start = topAnchor.position;
        for (int i = 0; i < N; i++)
        {
            float jitter = Random.Range(-0.02f, 0.02f);
            nodes[i].x = start + new Vector2(jitter, -restLength * i);
            nodes[i].v = Vector2.zero;
            nodes[i].m = nodeMass;
        }
        nodes[0].pinned = true;

        for (int i = 0; i < N - 1; i++)
            links.Add(new Link { i = i, j = i + 1, L0 = restLength, k = k, c = c, broken = false });

        if (linkPrefab != null)
            for (int i = 0; i < links.Count; i++)
                visuals.Add(Instantiate(linkPrefab, transform));
    }

    void Update()
    {
        if (released && !hasDropped)
        {
            hasDropped = true;
            StartCoroutine(SmoothDrop());
        }

        Simulate(Time.deltaTime);
        DrawLinks();
    }

    void Simulate(float frameDt)
    {
        int steps = substepsPerFrame;
        float dt = substepDt;

        for (int s = 0; s < steps; s++)
        {
            nodes[0].x = topAnchor.position;
            nodes[0].v = Vector2.zero;

            if (!released)
            {
                nodes[N - 1].x = bottomAnchor.position;
                nodes[N - 1].v = Vector2.zero;

                if (Time.time > releaseTime)
                {
                    released = true;

                    // 🔥 pre-stretch the bottom node downward before breaking
                    nodes[N - 1].x += new Vector2(0, -restLength * 0.4f);
                    nodes[N - 1].v = new Vector2(0, -50f);

                    BreakLastLink();
                }
            }

            // integrate
            for (int i = 0; i < N; i++)
            {
                if (nodes[i].pinned) continue;
                nodes[i].v += gravity * dt;
                nodes[i].v *= (1f - airFriction * dt);
                nodes[i].x += nodes[i].v * dt;
            }

            // constraint solve
            for (int it = 0; it < constraintIters; it++)
            {
                for (int e = 0; e < links.Count; e++)
                {
                    Link L = links[e];
                    if (L.broken) continue;

                    int i = L.i, j = L.j;
                    Vector2 xi = nodes[i].x, xj = nodes[j].x;
                    Vector2 d = xj - xi;
                    float dist = d.magnitude;
                    if (dist < 1e-6f) continue;
                    Vector2 n = d / dist;

                    float err = dist - L.L0;
                    float w1 = nodes[i].pinned ? 0f : 1f / nodes[i].m;
                    float w2 = nodes[j].pinned ? 0f : 1f / nodes[j].m;
                    float wsum = w1 + w2;
                    if (wsum <= 0f) continue;

                    Vector2 corr = (err / wsum) * n;
                    if (!nodes[i].pinned) nodes[i].x += corr * w1 * 0.8f;
                    if (!nodes[j].pinned) nodes[j].x -= corr * w2 * 0.8f;

                    Vector2 relv = nodes[j].v - nodes[i].v;
                    float reln = Vector2.Dot(relv, n);
                    float dampImpulse = L.c * reln;
                    if (!nodes[i].pinned) nodes[i].v += (dampImpulse * n) * (w1 / wsum);
                    if (!nodes[j].pinned) nodes[j].v -= (dampImpulse * n) * (w2 / wsum);
                }
            }
        }
    }

    void BreakLastLink()
    {
        if (fractured) return;
        fractured = true;

        int e = links.Count - 1;
        Link L = links[e];
        L.broken = true;
        links[e] = L;

        int i = L.i, j = L.j;
        Vector2 d = nodes[j].x - nodes[i].x;
        float dist = d.magnitude;
        Vector2 n = dist > 1e-6f ? d / dist : Vector2.up;
        float stretch = dist - L.L0;
        if (Mathf.Abs(stretch) < 0.05f) stretch = 0.05f;

        float E = 0.5f * L.k * stretch * stretch;
        float invMass = (nodes[i].pinned ? 0f : 1f / nodes[i].m)
                      + (nodes[j].pinned ? 0f : 1f / nodes[j].m);
        if (invMass <= 0f) return;

        // 🔥 multiply impulse for stronger effect
        float mu = Mathf.Sqrt(2f * alpha * E / invMass) * kickMultiplier;

        Vector2 impulseDir = -n;
        if (!nodes[i].pinned) nodes[i].v += (mu / nodes[i].m) * impulseDir;
        if (!nodes[j].pinned) nodes[j].v += (mu / nodes[j].m) * impulseDir;
    }

    void DrawLinks()
    {
        for (int i = 0; i < links.Count; i++)
        {
            var L = links[i];
            if (linkPrefab == null || i >= visuals.Count) continue;
            GameObject seg = visuals[i];

            if (L.broken) { seg.SetActive(false); continue; }

            Vector2 a = nodes[L.i].x;
            Vector2 b = nodes[L.j].x;
            Vector2 mid = (a + b) * 0.5f;
            Vector2 dir = b - a;
            float len = dir.magnitude;

            seg.transform.position = new Vector3(mid.x, mid.y, 0);
            seg.transform.up = dir.normalized;
            seg.transform.localScale = new Vector3(0.05f, len * 0.5f, 0.05f);

            float stretch = Mathf.Abs(len - L.L0);
            float t = Mathf.Clamp01(stretch * k / breakForce);
            seg.GetComponent<Renderer>().material.color =
                Color.Lerp(lowStressColor, highStressColor, t);
        }
    }

    IEnumerator SmoothDrop()
    {
        Vector3 start = bottomAnchor.position;
        Vector3 target = start + new Vector3(0, -4f, 0);
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            bottomAnchor.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
    }
}
