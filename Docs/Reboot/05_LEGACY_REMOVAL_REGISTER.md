# Dodge From Light — registre de retrait du legacy

Ce registre empêche la nouvelle architecture de devenir une couche supplémentaire
au-dessus de l’ancienne. Un système remplacé doit avoir un propriétaire, un point de
débranchement et une suppression vérifiable.

## Supprimé

### Awakenings

Statut : supprimé entièrement.

- six scripts et leurs métadonnées supprimés ;
- génération aléatoire et branchements de tour supprimés ;
- champs sérialisés retirés de `GameManager` et de son prefab ;
- exclusions particulières de Keeper et Trap supprimées ;
- cinq prefabs VFX et quatre matériaux exclusivement liés supprimés ;
- références retirées du projet C# généré présent dans le dépôt.

Le matériau `ConcentrateMaterial`, également utilisé par `Pf_Stone`, n’a pas été
détruit. Il a été renommé `StoneTrailMaterial` et déplacé dans les matériaux du piège
en conservant son GUID, donc le prefab existant reste lié correctement.

### Ancienne simulation Unity

Statut : débranchée du jeu.

- l’ancienne boucle de tours et `ScoutSystem` ont été supprimés ;
- les callbacks Unity ne résolvent plus les déplacements, visions, morts, cellules,
  gears ou plateformes ;
- téléporteurs, ressorts et pièges aléatoires utilisent le générateur déterministe du
  Core ;
- le `GameManager` importe la map, envoie des commandes explicites et applique
  uniquement les états et événements résolus ;
- le singleton historique `DodgeFromLight` a été renommé `GameContext`, ce qui évite
  la collision entre un type global et les namespaces de l’architecture ;
- aucun événement de tour legacy n’est émis pendant une partie Core.

### Contenu gameplay porté

Statut : propriétaire unique dans `DodgeFromLight.Core`.

- les 12 ennemis et leurs zones d’effet ;
- les 10 types de cellules, y compris ressorts chaînés et bombes temporisées ;
- les 9 gears retenus, leurs durées et leurs effets ;
- les plateformes mobiles et le transport du joueur ;
- inventaire, checkpoint, ancre d’épée, statuts, aléatoire et fin de partie ;
- hash complet de l’état pour replay, undo et validation déterministe.

Les scripts `Ennemy`, `MovingPlatform`, `Needle`, `BmbeController` et les classes de
gears encore attachés aux prefabs ne sont plus des autorités de gameplay pendant une
partie. Leurs composants sont désactivés au chargement et leurs objets servent de vues
pour conserver tous les modèles, animations, matériaux et VFX existants.

Point de suppression : remplacer progressivement ces composants sérialisés par des
vues dédiées, prefab par prefab, puis supprimer chaque classe historique dans le même
changement. Aucun de ces composants ne doit retrouver une responsabilité de règle.

### Chargeur et sources de maps

L’import unitaire et l’import par lot sont désormais possédés par
`DodgeFromLight.Infrastructure.Maps`. Le lot distingue le gameplay, l’environnement et
la preview, puis conserve explicitement chaque variante en conflit.

Le chargeur de fichiers `StreamingAssets` reste actif à la frontière Unity, puis son
résultat en mémoire est immédiatement converti vers le modèle cible. Les 30 sources
jouables ont été importées et acceptées par le ruleset complet. `House` reste un asset
visuel hors catalogue jouable ; le hub multijoueur ne revient pas.

Quatre IDs, ceux des niveaux 23 à 26, possèdent deux environnements visuels différents
et ne peuvent pas être nettoyés sans décision de contenu.

Point de suppression : après résolution des quatre conflits, génération du catalogue
cible et branchement de `UnityPresentation` sur ce catalogue, retirer dans le même
changement l’ancien chargeur, ses adaptateurs et les copies `StreamingAssets` devenues
sans consommateur.

## Règle pour les prochaines tranches

Un ancien système n’est conservé que s’il possède encore un consommateur actif
identifié. Les copies « au cas où », adaptateurs sans échéance, types non référencés et
assets devenus orphelins sont supprimés pendant la tranche qui les remplace.
