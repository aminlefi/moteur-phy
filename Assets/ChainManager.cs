using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GestionnaireDeChaine : MonoBehaviour
{
    [Header("Paramètres de la Chaîne")]
    public int NombreDePoints = 20;
    public float MasseDuPoint = 0.08f;
    public float LongueurDeRepos = 0.15f;
    public float ConstanteDeRaideur = 600f;
    public float CoefficientDAmortissement = 0.06f;
    public float CoefficientDeFrottementDeLAir = 0.0015f;
    public float ForceDeRupture = 80f;
    [Range(0, 2)] public float CoefficientDEnergie = 1.0f;
    public float MultiplicateurDImpulsion = 14f;

    [Header("Intégration Temporelle")]
    public float PasDeSousEtape = 1f / 600f;
    public int NombreDeSousEtapesParImage = 8;
    public int NombreDIterationsDeContrainte = 6;
    public Vector2 AccelerationGravitationnelle = new Vector2(0, -9.81f);

    [Header("Points d'Ancrage")]
    public Vector3 PositionAncrageSuperieur = new Vector3(0, 5, 0);
    public Vector3 PositionAncrageInferieur = new Vector3(0, 2, 0);
    public float DistanceDeChute = 4f;
    public float TempsDeLiberation = 1.5f;

    // internes
    struct PointMateriel
    {
        public Vector2 Position;
        public Vector2 Vitesse;
        public float Masse;
        public bool EstFixe;
    }
    struct LiaisonMecanique
    {
        public int IndexA, IndexB;
        public float LongueurDeRepos, ConstanteDeRaideur, CoefficientDAmortissement;
        public bool EstRompue;
    }

    PointMateriel[] Points;
    List<LiaisonMecanique> Liaisons = new();
    bool EstLiberee = false, ADejaChute = false, EstFracturee = false;

    void Start()
    {
        Points = new PointMateriel[NombreDePoints];
        Vector2 PositionDepart = PositionAncrageSuperieur;

        for (int i = 0; i < NombreDePoints; i++)
        {
            float DeviationLaterale = Random.Range(-0.015f, 0.015f);
            Points[i].Position = PositionDepart + new Vector2(DeviationLaterale, -LongueurDeRepos * i);
            Points[i].Vitesse = Vector2.zero;
            Points[i].Masse = MasseDuPoint;
        }
        Points[0].EstFixe = true;

        for (int i = 0; i < NombreDePoints - 1; i++)
            Liaisons.Add(new LiaisonMecanique
            {
                IndexA = i,
                IndexB = i + 1,
                LongueurDeRepos = LongueurDeRepos,
                ConstanteDeRaideur = ConstanteDeRaideur,
                CoefficientDAmortissement = CoefficientDAmortissement,
                EstRompue = false
            });
    }

    void Update()
    {
        if (EstLiberee && !ADejaChute)
        {
            ADejaChute = true;
            StartCoroutine(ChuteProgressive());
        }

        Simuler(Time.deltaTime);
    }

    void Simuler(float DureeImage)
    {
        int SousEtapes = NombreDeSousEtapesParImage;
        float Δt = PasDeSousEtape;

        // ----- BOUCLE DE SOUS-INTÉGRATION (intégration plus stable) -----
        for (int s = 0; s < SousEtapes; s++)
        {
            // ---- 1) FIXER LE POINT D’ANCRAGE SUPÉRIEUR ----
            Points[0].Position = PositionAncrageSuperieur;
            Points[0].Vitesse = Vector2.zero;

            // ---- 2) SI LA CHAÎNE N’EST PAS ENCORE LIBÉRÉE, MAINTENIR LE POINT INFÉRIEUR ----
            if (!EstLiberee)
            {
                Points[NombreDePoints - 1].Position = PositionAncrageInferieur;
                Points[NombreDePoints - 1].Vitesse = Vector2.zero;

                // ---- 3) LIBÉRATION APRÈS LE TEMPS DONNÉ + IMPULSION INITIALE ----
                if (Time.time > TempsDeLiberation)
                {
                    EstLiberee = true;

                    // petite translation vers le bas pour créer un étirement
                    Points[NombreDePoints - 1].Position += new Vector2(0, -LongueurDeRepos * 0.25f);

                    // vitesse initiale vers le bas au moment de la libération
                    Points[NombreDePoints - 1].Vitesse = new Vector2(0, -25f);

                    // rupture de la dernière liaison -> génération du "snap"
                    RompreDerniereLiaison();
                }
            }

            // ----- 4) INTÉGRATION DES LOIS DE NEWTON POUR CHAQUE POINT -----
            for (int i = 0; i < NombreDePoints; i++)
            {
                if (Points[i].EstFixe) continue;

                // 4.1 - Appliquer la gravité (mécanique du point matériel)
                Points[i].Vitesse += AccelerationGravitationnelle * Δt;

                // 4.2 - Appliquer le frottement de l’air (amortissement global)
                Points[i].Vitesse *= (1f - CoefficientDeFrottementDeLAir * Δt);

                // 4.3 - Mettre à jour la position (intégration d’Euler)
                Points[i].Position += Points[i].Vitesse * Δt;
            }

            // ----- 5) BOUCLE DE SATISFACTION DES CONTRAINTES (itérations Gauss–Seidel) -----
            for (int it = 0; it < NombreDIterationsDeContrainte; it++)
            {
                for (int e = 0; e < Liaisons.Count; e++)
                {
                    LiaisonMecanique L = Liaisons[e];
                    if (L.EstRompue) continue;

                    // indices des deux points reliés
                    int i = L.IndexA, j = L.IndexB;

                    // positions actuelles
                    Vector2 PositionA = Points[i].Position;
                    Vector2 PositionB = Points[j].Position;

                    // vecteur reliant les deux points
                    Vector2 Direction = PositionB - PositionA;
                    float Distance = Direction.magnitude;

                    // éviter division par zéro
                    if (Distance < 1e-6f) continue;

                    // vecteur normalisé (direction du ressort)
                    Vector2 DirectionNormale = Direction / Distance;

                    // ---- 5.1 - CALCUL DE L’ERREUR DE CONTRAINTE ----
                    float Erreur = Distance - L.LongueurDeRepos;

                    // masses inverses (si fixé → 0)
                    float InverseMasseA = Points[i].EstFixe ? 0f : 1f / Points[i].Masse;
                    float InverseMasseB = Points[j].EstFixe ? 0f : 1f / Points[j].Masse;

                    float SommeInverses = InverseMasseA + InverseMasseB;
                    if (SommeInverses <= 0f) continue;

                    // ---- 5.2 - CALCUL DE LA CORRECTION POUR RÉTABLIR LA LONGUEUR ----
                    Vector2 Correction = (Erreur / SommeInverses) * DirectionNormale;

                    const float FacteurDeRelaxation = 0.85f;

                    // appliquer correction pondérée aux positions
                    if (!Points[i].EstFixe) Points[i].Position += Correction * InverseMasseA * FacteurDeRelaxation;
                    if (!Points[j].EstFixe) Points[j].Position -= Correction * InverseMasseB * FacteurDeRelaxation;

                    // ---- 5.3 - AMORTISSEMENT DE LA VITESSE RELATIVE (amortissement interne du ressort) ----
                    Vector2 VitesseRelative = Points[j].Vitesse - Points[i].Vitesse;

                    // composante normale de la vitesse relative (alignée avec la liaison)
                    float ComposanteNormaleVitesse = Vector2.Dot(VitesseRelative, DirectionNormale);

                    // impulsion amortie → dissiper une partie de l’énergie interne
                    float ImpulsionAmortie = L.CoefficientDAmortissement * ComposanteNormaleVitesse * 0.5f;

                    // application pondérée aux vitesses
                    if (!Points[i].EstFixe)
                        Points[i].Vitesse += (ImpulsionAmortie * DirectionNormale) * (InverseMasseA / SommeInverses);

                    if (!Points[j].EstFixe)
                        Points[j].Vitesse -= (ImpulsionAmortie * DirectionNormale) * (InverseMasseB / SommeInverses);
                }
            }
        }
    }


    void RompreDerniereLiaison()
    {
        if (EstFracturee) return;
        EstFracturee = true;

        int e = Liaisons.Count - 1;
        LiaisonMecanique L = Liaisons[e];
        L.EstRompue = true;
        Liaisons[e] = L;

        int i = L.IndexA, j = L.IndexB;
        Vector2 Direction = Points[j].Position - Points[i].Position;
        float Distance = Direction.magnitude;
        if (Distance < 1e-6f) return;
        Vector2 Normale = Direction / Distance;

        float Allongement = Distance - L.LongueurDeRepos;
        if (Mathf.Abs(Allongement) < 0.05f) Allongement = 0.05f;

        float Energie = 0.5f * L.ConstanteDeRaideur * Allongement * Allongement;
        float MasseInversee = (Points[i].EstFixe ? 0f : 1f / Points[i].Masse)
                            + (Points[j].EstFixe ? 0f : 1f / Points[j].Masse);
        if (MasseInversee <= 0f) return;

        float Impulsion = Mathf.Sqrt(2f * CoefficientDEnergie * Energie / MasseInversee) * MultiplicateurDImpulsion;
        Vector2 DirectionImpulsion = (-Normale + new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.05f, 0.05f))).normalized;

        if (!Points[i].EstFixe) Points[i].Vitesse += (Impulsion / Points[i].Masse) * DirectionImpulsion;
        if (!Points[j].EstFixe) Points[j].Vitesse += (Impulsion / Points[j].Masse) * DirectionImpulsion;
    }

    IEnumerator ChuteProgressive()
    {
        Vector3 Depart = PositionAncrageInferieur;
        Vector3 Cible = Depart + new Vector3(0, -DistanceDeChute, 0);
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            PositionAncrageInferieur = Vector3.Lerp(Depart, Cible, t);
            yield return null;
        }
    }

    void OnDrawGizmos()
    {
        if (Points == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(PositionAncrageSuperieur, 0.08f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(PositionAncrageInferieur, 0.08f);

        for (int e = 0; e < Liaisons.Count; e++)
        {
            var L = Liaisons[e];
            if (L.EstRompue) continue;
            Vector2 a = Points[L.IndexA].Position;
            Vector2 b = Points[L.IndexB].Position;
            float Étirement = Mathf.Abs((b - a).magnitude - L.LongueurDeRepos);
            Gizmos.color = Color.Lerp(Color.white, Color.red, Mathf.Clamp01(Étirement * ConstanteDeRaideur / ForceDeRupture));
            Gizmos.DrawLine(new Vector3(a.x, a.y, 0), new Vector3(b.x, b.y, 0));
        }
    }
}
