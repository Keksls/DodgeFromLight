# Dodge From Light — migration du catalogue de maps

## État vérifié

Le dossier `StreamingAssets` contient 30 sources complètes, chacune avec `grid.json`,
`metadata.json` et `preview.png`. Elles représentent 26 IDs de map uniques.

L’inventaire figé, avec les SHA-256 des trois fichiers de chaque source, se trouve dans
`legacy-map-source-inventory.json`. Il permet de détecter toute modification ultérieure
du contenu legacy pendant la migration.

## Les quatre IDs dupliqués

Les niveaux 23 à 26 existent à la racine et dans `Tutorial`. Pour chaque paire :

- le gameplay importé est identique ;
- les métadonnées sont identiques ;
- la preview est identique ;
- l’environnement visuel est différent.

Les quatre copies racine partagent le même environnement. Les quatre copies `Tutorial`
partagent également un environnement commun, différent de celui de la racine. Ce ne
sont donc pas des doublons complets supprimables automatiquement.

## Règle de l’import par lot

`LegacyMapBatchImporter` compare séparément :

1. le hash canonique du gameplay et des références d’assets ;
2. l’environnement JSON canonique ;
3. les octets de la preview.

Deux sources ne sont fusionnées que si ces trois couches sont équivalentes. Le résultat
conserve alors tous leurs libellés de provenance.

Si une couche diffère sous le même ID, `LegacyMapImportConflict` conserve les deux
packages et leurs previews. Le lot est invalide et aucune version n’est choisie
silencieusement.

## Décision de contenu encore nécessaire

Avant de générer le catalogue cible définitif, il faut choisir l’environnement des
niveaux 23 à 26 : variante racine, variante `Tutorial`, ou deux variantes visuelles
assumées avec des IDs distincts. Cette décision n’affecte pas le cœur des puzzles.

Les sources `StreamingAssets` restent actives jusqu’à ce que le nouveau chargeur lise
le catalogue cible et que cette collision visuelle soit résolue.
