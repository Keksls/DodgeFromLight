# Dodge From Light — règles figées du Scout

Date de décision : 28 juillet 2026.

Cette tranche remplace l’observation du `Scoute` legacy par une règle déterministe
indépendante de Unity.

## Données conservées

- L’identifiant cible est `enemy.scout`.
- `PO` devient la portée de vision `SightRange`.
- `TA` devient le nombre de micro-actions `ActionsPerTurn`.
- Les valeurs viennent de chaque map ; elles ne sont pas remplacées par les anciens
  defaults implicites.
- Les waypoints sont développés en un trajet cyclique, cardinal et cellule par cellule.

## Résolution d’un tour

1. La commande valide du joueur est appliquée.
2. Les Scouts sont résolus dans l’ordre stable de leur définition dans la map.
3. Pour chaque micro-action :
   - avancer vers la prochaine cellule si le Scout regarde déjà dans sa direction ;
   - sinon tourner vers cette cellule, sans avancer ;
   - si la cellule est bloquée, inverser la patrouille et l’orientation, sans avancer.
4. Une rotation, une inversion ou un déplacement consomme une micro-action.
5. La vision est recalculée après chaque micro-action.
6. Un Scout sans déplacement disponible vérifie sa vision une fois après l’action du joueur.
7. La détection et la collision avec le Scout causent la défaite avant la validation de la sortie.

## Vision

Le Scout voit sa propre cellule puis une ligne droite dans son orientation, jusqu’à
`SightRange`. La grille, un élément bloquant ou un autre ennemi arrête la ligne de vue.
La cellule bloquante et les cellules situées derrière elle ne sont pas dangereuses.

Les miroirs et gears dynamiques seront ajoutés avec leur propre tranche de simulation.
Une map qui en contient reste refusée par ce ruleset au lieu de les ignorer.

## Limite volontaire

Les routes de plusieurs ennemis ne peuvent pas encore se chevaucher. Ces maps sont
refusées explicitement jusqu’à la décision générale sur les collisions simultanées.
Cette limite ne concerne aucun des dix Scouts présents dans le corpus legacy actuel.
