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

## Remplacé dans le Core, encore actif dans la présentation legacy

### Scout

La simulation déterministe du Scout remplace désormais sa règle de gameplay dans le
nouveau Core. Les éléments suivants restent temporairement actifs parce que le jeu
Unity courant les instancie encore directement :

- `Scoute.cs` ;
- les branches `EnnemyType.Scoute` de `Grid` et `GridController` ;
- le prefab de présentation du Scout ;
- les outils de placement de l’éditeur legacy.

Ils ne peuvent pas être supprimés isolément : `SerializableEnnemy.ToEnnemy` construit
encore un composant `Scoute`, et l’ancien `GameManager` attend un `Ennemy` animé par
coroutine.

Point de suppression : dès que `UnityPresentation` construit la vue du Scout depuis
`EnemyState` et consomme `EnemyMoved`, `EnemyTurned` et `PlayerDetected`, ces branches
legacy doivent être supprimées dans le même changement.

### Chargeur et sources de maps

L’import unitaire et l’import par lot sont désormais possédés par
`DodgeFromLight.Infrastructure.Maps`. Le lot distingue le gameplay, l’environnement et
la preview, puis conserve explicitement chaque variante en conflit.

Le chargeur legacy et `StreamingAssets` restent actifs parce que la présentation
courante les consomme encore. Quatre IDs, ceux des niveaux 23 à 26, possèdent deux
environnements visuels différents et ne peuvent pas être nettoyés sans décision de
contenu.

Point de suppression : après résolution des quatre conflits, génération du catalogue
cible et branchement de `UnityPresentation` sur ce catalogue, retirer dans le même
changement l’ancien chargeur, ses adaptateurs et les copies `StreamingAssets` devenues
sans consommateur.

## Règle pour les prochaines tranches

Un ancien système n’est conservé que s’il possède encore un consommateur actif
identifié. Les copies « au cas où », adaptateurs sans échéance, types non référencés et
assets devenus orphelins sont supprimés pendant la tranche qui les remplace.
