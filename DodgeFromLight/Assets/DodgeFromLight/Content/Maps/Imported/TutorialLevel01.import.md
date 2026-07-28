# Import de `Tutorial/Level 1`

Source :

- `Assets/StreamingAssets/Tutorial/Level 1/grid.json`
- `Assets/StreamingAssets/Tutorial/Level 1/metadata.json`

Résultat :

- schéma cible `2` ;
- ruleset initial `1` ;
- grille `5x6` et 30 cellules conservées ;
- start `(2,0)` et exit `(2,5)` conservés ;
- deux `enemy.scout` conservés avec orientations, statistiques et waypoints ;
- environnement legacy conservé hors du cœur logique ;
- aucune plateforme, aucun gear et aucune cellule spéciale dans cette map.

La map est désormais compatible avec la tranche déterministe du Scout : les deux
patrouilles, leur portée, leurs actions et leur détection sont prises en compte.

La source legacy n’est ni modifiée ni remplacée.
