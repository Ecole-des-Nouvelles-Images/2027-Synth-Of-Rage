using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SynthOfRage.Editor
{
    public sealed class SORShaderGUI : ShaderGUI
    {
        private const float HelpWidth = 22f;

        private static readonly Color HelpColor = new Color(0.1f, 1f, 0.55f, 0.95f);
        private static readonly Color HelpHoverColor = new Color(0.1f, 1f, 0.55f, 0.45f);
        private static GUIStyle helpStyle;


        private static readonly Dictionary<string, string> Tooltips =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "_BaseMap", "Texture couleur principale du matériau." },
                { "_BaseColor", "Teinte multipliée avec la texture de base." },
                { "_NormalMap", "Normal Map utilisée pour modifier visuellement l’éclairage." },
                { "_NormalStrength", "Intensité de la Normal Map." },
                { "_LightBands", "Nombre de niveaux utilisés pour quantifier l’éclairage toon." },
                { "_LightBias", "Décale globalement la séparation entre lumière et ombre." },
                { "_ShadowColor", "Couleur appliquée aux zones les plus sombres." },
                { "_DirectLightStrength", "Intensité de la lumière principale." },
                { "_AmbientStrength", "Intensité de l’éclairage ambiant appliqué au matériau." },
                { "_ReceiveShadowStrength", "Intensité des ombres reçues par le matériau." },
                { "_UseStepPattern", "Active la déformation artistique entre les niveaux de lumière." },
                { "_StepPattern", "Texture utilisée pour rendre les transitions de lumière irrégulières." },
                { "_StepPatternWorldScale", "Contrôle la fréquence du Step Pattern dans le monde." },
                { "_StepPatternWorldOffset", "Décale le Step Pattern dans le monde." },
                { "_StepPatternStrength", "Intensité avec laquelle le pattern déforme les niveaux de lumière." },
                { "_RimPatternStrength", "Intensité avec laquelle le pattern déforme la limite du Rim." },
                { "_StepPatternContrast", "Augmente ou réduit le contraste du Step Pattern." },
                { "_StepPatternProjectionBlend", "Contrôle la netteté du mélange triplanaire du Step Pattern." },
                { "_StepPatternInvert", "Inverse les zones claires et sombres du Step Pattern." },
                { "_HalftoneStrength", "Intensité générale des dots." },
                { "_HalftoneColor", "Couleur appliquée aux dots." },
                { "_HalftoneSize", "Taille des cellules quand les dots utilisent l’écran comme repère." },
                { "_HalftoneLevels", "Nombre de niveaux de taille utilisés pour les dots." },
                { "_HalftoneMinRadius", "Taille minimale d’un dot." },
                { "_HalftoneMaxRadius", "Taille maximale d’un dot." },
                { "_HalftoneAngle", "Rotation du motif de dots." },
                { "_HalftoneWorldSpace", "OFF : dots fixés à l’écran ; ON : dots fixés dans le monde." },
                { "_HalftoneWorldScale", "Contrôle la densité des dots lorsqu’ils sont fixés au monde." },
                { "_HalftoneWorldOffset", "Décale le motif de dots dans le monde." },
                { "_HalftoneWorldProjectionBlend", "Netteté du mélange triplanaire des dots world-space." },
                { "_PosterizeSteps", "Nombre de niveaux de couleur conservés par la posterisation finale." },
                { "_PosterizeStrength", "Intensité de la posterisation finale." },
                { "_SpecularColor", "Couleur du reflet toon." },
                { "_SpecularStrength", "Intensité du reflet toon." },
                { "_SpecularPower", "Contrôle la taille et la concentration du reflet." },
                { "_SpecularThreshold", "Seuil à partir duquel le reflet apparaît." },
                { "_RimColor", "Couleur de la lumière de contour." },
                { "_RimStrength", "Intensité de la lumière de contour." },
                { "_RimPower", "Contrôle la largeur et la concentration du Rim." },
                { "_RimThreshold", "Seuil à partir duquel le Rim devient visible." },
                { "_AdditionalLightStrength", "Intensité des lumières Point/Spot supplémentaires." },
                { "_OutlineSprites3D", "Active la prise en compte des Sprite3D dans l’outline." },
                { "_OutlineColor", "Couleur principale de l’outline." },
                { "_OutlineThickness", "Épaisseur de base de l’outline en pixels." },
                { "_OutlineOpacity", "Opacité générale de l’outline." },
                { "_NearThicknessMultiplier", "Multiplie l’épaisseur lorsque l’objet est proche." },
                { "_DistanceNear", "Distance à partir de laquelle l’objet est considéré proche." },
                { "_DistanceFar", "Distance à partir de laquelle l’objet est considéré loin." },
                { "_FarThicknessMultiplier", "Multiplie l’épaisseur des contours lointains." },
                { "_DistanceFalloff", "Contrôle la courbe de transition entre proche et lointain." },
                { "_DepthThreshold", "Différence de profondeur minimale nécessaire pour créer un contour." },
                { "_DepthSoftness", "Adoucit la détection autour du seuil de profondeur." },
                { "_DepthStrength", "Intensité générale de la détection des contours." },
                { "_SlopeCompensation", "Réduit les faux contours produits par les surfaces inclinées." },
                { "_SlopeSampleReject", "Ignore les variations trop fortes pour être considérées comme une pente." },
                { "_UseDistanceDirtyFade", "Active l’adaptation du style graffiti selon la distance." },
                { "_DirtyFadeStart", "Distance où les petits détails commencent à être réduits." },
                { "_DirtyFadeEnd", "Distance où la réduction des petits détails atteint son maximum." },
                { "_FarDirtyMin", "Quantité minimale de petits détails conservée très loin." },
                { "_MacroFadeStart", "Distance où les grosses imperfections commencent à diminuer." },
                { "_MacroFadeEnd", "Distance où la réduction des grosses imperfections est maximale." },
                { "_FarMacroMinimum", "Quantité minimale de grosses imperfections conservée au loin." },
                { "_NearGrandBoost", "Renforce les grosses imperfections lorsque la caméra est proche." },
                { "_GrandBoostDistance", "Distance sur laquelle le Near Grand Boost agit." },
                { "_FarGraffitiShellWidth", "Épaisseur supplémentaire du style graffiti au loin." },
                { "_FarGraffitiShellStrength", "Intensité du style graffiti conservé au loin." },
                { "_UseOutlineImperfections", "Active les imperfections avancées du style Shibuya Punk." },
                { "_ImperfectionPattern", "Texture servant à créer les irrégularités de peinture." },
                { "_ImperfectionFullTriplanar", "Utilise les trois axes triplanaires pour un meilleur rendu, plus coûteux." },
                { "_ImperfectionWorldScale", "Contrôle la fréquence de la texture graffiti dans le monde." },
                { "_ImperfectionWorldOffset", "Décale la texture graffiti dans le monde." },
                { "_ImperfectionSeed", "Change la répartition pseudo-aléatoire des imperfections." },
                { "_ImperfectionContrast", "Renforce ou réduit le contraste de la texture graffiti." },
                { "_ImperfectionProjectionBlend", "Contrôle la netteté du mélange triplanaire des imperfections." },
                { "_MacroScale", "Taille et fréquence des grosses masses de peinture." },
                { "_MacroStrength", "Intensité des grosses masses de peinture." },
                { "_ChunkBoost", "Distance maximale à laquelle les grosses masses dépassent du contour." },
                { "_ChunkThreshold", "Contrôle la rareté des grosses masses de peinture." },
                { "_ThicknessJitter", "Ajoute des variations aléatoires d’épaisseur." },
                { "_BreakupStrength", "Contrôle les trous et coupures façon pinceau sec." },
                { "_BreakupThreshold", "Détermine quelles parties sont supprimées par le dry brush." },
                { "_BreakupSoftness", "Adoucit les coupures créées par le dry brush." },
                { "_CoreIntegrity", "Protège le centre du contour contre les coupures." },
                { "_OpacityJitter", "Ajoute des variations d’opacité dans la peinture." },
                { "_SpikeLength", "Longueur maximale des pics et coups de pinceau." },
                { "_SpikeThreshold", "Contrôle la rareté des pics : plus haut = plus rare." },
                { "_SpikeDirectionality", "Rend les pics plus ou moins directionnels et pointus." },
                { "_DripTexture", "Texture ou atlas contenant les formes de coulées." },
                { "_DripTextureColumns", "Nombre de variantes de gouttes présentes horizontalement dans l’atlas." },
                { "_DripTextureContrast", "Contraste appliqué au masque de la goutte." },
                { "_DripTextureThreshold", "Seuil définissant quelles zones de la texture deviennent de la peinture." },
                { "_DripTextureSoftness", "Adoucit le contour du masque de goutte." },
                { "_DripTextureDilation", "Élargit horizontalement le masque de goutte pour améliorer sa lisibilité." },
                { "_DripTextureInvert", "Inverse le noir et le blanc de la texture de goutte." },
                { "_DripLength", "Longueur moyenne d’une coulée en pixels." },
                { "_DripMinLength", "Longueur minimale autorisée pour une coulée." },
                { "_DripSearchHeight", "Distance verticale maximale utilisée pour rechercher le bord source d’une goutte." },
                { "_DripWorldSpacing", "Espace moyen entre les emplacements possibles des gouttes dans le monde." },
                { "_DripWorldWidth", "Largeur de la zone d’accroche d’une goutte dans le monde." },
                { "_DripWidth", "Multiplie la largeur affichée de la texture de goutte." },
                { "_DripFarWidthBoost", "Élargit les gouttes lointaines pour éviter leur disparition." },
                { "_DripLengthVariation", "Ajoute des différences de longueur entre les gouttes." },
                { "_DripLongChance", "Probabilité qu’une goutte devienne exceptionnellement longue." },
                { "_DripLongMultiplier", "Multiplie la longueur des gouttes désignées comme longues." },
                { "_DripThreshold", "Contrôle la rareté des gouttes : plus haut = moins de gouttes." },
                { "_DripOpacity", "Opacité générale des coulées." },
                { "_OversprayStrength", "Intensité des petites projections de peinture autour du contour." },
                { "_OversprayWidth", "Distance maximale des projections autour du contour." },
                { "_OversprayThreshold", "Contrôle la quantité de projections visibles." },
                { "_OverspraySoftness", "Adoucit l’apparition des projections." },
                { "_AccentColor", "Deuxième couleur utilisée sur certaines imperfections." },
                { "_AccentStrength", "Intensité de la couleur secondaire." },
                { "_AccentThreshold", "Contrôle la rareté des zones utilisant la couleur secondaire." },
                { "_AccentSoftness", "Adoucit la transition de la couleur secondaire." },
                { "_AccentOuterBias", "Favorise la couleur secondaire vers l’extérieur de l’outline." },
                { "_OutlineMask", "Texture permettant de conserver ou supprimer certaines parties de l’outline." },
                { "_MaskStrength", "Intensité de l’influence du masque." },
                { "_MaskWorldScale", "Échelle du masque dans le monde." },
                { "_MaskWorldOffset", "Décale le masque dans le monde." },
                { "_MaskThreshold", "Seuil noir/blanc utilisé pour découper le masque." },
                { "_MaskSoftness", "Adoucit les transitions du masque." },
                { "_MaskInvert", "Inverse les zones conservées et supprimées." },
                { "_MaskProjectionBlend", "Contrôle la netteté du mélange triplanaire du masque." },
                { "_MainTex", "Texture affichée par le SpriteRenderer." },
                { "_Color", "Teinte multipliée avec la texture du sprite." },
                { "_ReceiveShadows", "Active ou désactive les ombres reçues." },
                { "_CastShadows", "Active ou désactive les ombres projetées." },
                { "_ShadowAlphaClip", "Alpha minimum nécessaire pour qu’un pixel projette une ombre." },
                { "_EdgeWidth", "Distance autour de l’alpha utilisée pour détecter les bords du sprite." },
                { "_EdgeSensitivity", "Sensibilité de la détection des bords de transparence." },
                { "_EdgeLightBoost", "Quantité de lumière supplémentaire ajoutée sur les bords." },
                { "_SpriteDepthSortEpsilon", "Tolérance évitant les erreurs de tri entre sprites presque à la même profondeur." },
                { "_DepthOcclusionAlphaCutoff", "Alpha minimum nécessaire pour qu’un pixel puisse cacher un Sprite3D derrière lui." },
                { "_SpriteOutlineEnabled", "Active individuellement la participation du sprite à l’outline fullscreen." },
            };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("SOR Shader", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Survolez le ? à droite d'une propriété pour afficher son aide.", EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            foreach (MaterialProperty property in properties)
            {
                if ((property.propertyFlags & UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector) != 0)
                    continue;

                DrawProperty(materialEditor, property);
            }

            EditorGUILayout.Space(8);
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }

        private static void DrawProperty(MaterialEditor materialEditor, MaterialProperty property)
        {
            float height = materialEditor.GetPropertyHeight(property, property.displayName);
            Rect fullRect = EditorGUILayout.GetControlRect(true, height);

            bool hasTooltip = Tooltips.TryGetValue(property.name, out string tooltip);

            Rect propertyRect = fullRect;
            if (hasTooltip)
                propertyRect.width = Mathf.Max(0f, propertyRect.width - HelpWidth - 2f);

            materialEditor.ShaderProperty(propertyRect, property, property.displayName);

            if (!hasTooltip)
                return;

            Rect helpRect = new Rect(
                fullRect.xMax - HelpWidth,
                fullRect.y,
                HelpWidth,
                EditorGUIUtility.singleLineHeight
            );

            bool hovered = helpRect.Contains(Event.current.mousePosition);

            Rect badgeRect = new Rect(
                helpRect.x + 1f,
                helpRect.y + 1f,
                helpRect.width - 2f,
                helpRect.height - 2f
            );

            EditorGUI.DrawRect(
                badgeRect,
                hovered ? HelpHoverColor : HelpColor
            );

            if (helpStyle == null)
            {
                helpStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(0, 0, 0, 0)
                };

                helpStyle.normal.textColor = Color.black;
                helpStyle.hover.textColor = Color.black;
            }

            GUI.Label(
                helpRect,
                new GUIContent("?", tooltip),
                helpStyle
            );

            if (hovered)
                EditorGUIUtility.AddCursorRect(helpRect, MouseCursor.Link);
        }
    }
}
