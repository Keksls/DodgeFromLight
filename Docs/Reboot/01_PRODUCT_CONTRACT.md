# Dodge From Light — contrat produit du reboot

Statut : cadrage validé, aucune implémentation commencée.

Date de référence : 28 juillet 2026.

## Vision

`Dodge From Light` devient un puzzle tactique solo, déterministe et lisible :

> Chaque action du joueur fait avancer le monde. Observer les trajectoires et les zones de lumière, manipuler le plateau avec ses équipements, puis trouver une route plus élégante que la précédente.

Le premier passage doit rester accessible. L’optimisation arrive ensuite naturellement grâce au nombre d’actions, aux replays, aux fantômes et aux classements. Le jeu ne doit pas imposer la pression du chronomètre pendant la découverte d’un puzzle.

## Piliers

1. **Comprendre avant d’agir**
   - Les prochaines positions, zones dangereuses et changements d’état doivent être prévisibles.
   - Les règles doivent être explicables sans connaître leur implémentation.

2. **Rejouer pour optimiser**
   - Recommencer et annuler sont instantanés.
   - Une solution est un flux déterministe de commandes, donc un replay vérifiable.
   - La première réussite, l’optimisation en actions et le speedrun sont trois motivations compatibles, mais distinctes.

3. **Manipuler la lumière et le plateau**
   - Les ennemis, pièges, cellules, équipements, plateformes et capacités existants forment la boîte à outils du puzzle.
   - Les équipements doivent produire des décisions intéressantes, pas seulement des effets visuels.

4. **Créer et partager**
   - L’éditeur utilise exactement les mêmes règles et données que le jeu.
   - La campagne officielle apprend le langage du jeu et fournit une raison d’acheter le jeu même avant qu’une communauté UGC existe.

5. **Exprimer son identité sans MMO**
   - Les skins, armes, ailes, ornements et familiers restent valorisés.
   - Ils apparaissent dans le jeu, les replays, les fantômes, les profils de créateur et les classements.
   - Aucun cosmétique ne modifie la solvabilité d’une map.

## Contrat de conservation

Le reboot conserve :

- les 12 types d’ennemis ;
- les 9 types de gears ;
- les types de cellules, les pièges et les plateformes mobiles ;
- les capacités de puzzle existantes qui seront confirmées dans la matrice de parité ;
- les 26 puzzles uniques et les variantes d’environnement encore en arbitrage ;
- les environnements, décors, modèles, animations, sons et VFX exploitables ;
- le catalogue cosmétique existant, dont les familiers ;
- la capacité de créer, tester, sauvegarder et partager une map.

« Conserver » signifie préserver l’identité et l’utilité de puzzle. Cela ne signifie pas reproduire les bugs, les courses entre coroutines, les dépendances globales ou les comportements accidentels.

Les identifiants legacy sont conservés par l’importeur afin de migrer les maps et les sauvegardes. Le nouveau runtime n’utilise pas les anciennes classes comme fondation.

## Décisions déjà prises

| Sujet | Décision |
|---|---|
| Mode principal | Solo |
| Cible prioritaire | Joueurs d’optimisation et de speedrun, sans exclure la première résolution occasionnelle |
| Créateur de maps | Conservé comme pilier de rétention, après un socle de campagne convaincant |
| Hub MMO | Retiré de la cible produit |
| Multijoueur temps réel | Retiré tant qu’aucun besoin de gameplay ne le justifie |
| Awakenings | Supprimés entièrement du nouveau gameplay |
| Familier contrôlable | Supprimé |
| Familiers visuels | Conservés comme compagnons cosmétiques |
| Serveur propriétaire | Retiré de la cible initiale |
| Steam | Identité, Workshop, classements, Cloud et succès via des adaptateurs dédiés, plus tard dans la migration |
| Fugui | UI cible, isolée du gameplay |
| Ancien code | Spécification observable et source de migration, pas architecture à prolonger |

## Boucle de jeu cible

1. Le joueur choisit une map officielle ou communautaire.
2. Il inspecte le plateau, les cycles, les trajectoires et les zones de danger.
3. Il exécute des commandes discrètes : déplacement, attente, capacité ou gear.
4. La simulation résout le tour et produit une liste ordonnée d’événements.
5. La présentation anime ces événements sans modifier les règles.
6. Le joueur réussit, annule ou recommence instantanément.
7. Après la première réussite, le jeu propose :
   - un objectif d’actions ;
   - un défi de temps séparé ;
   - un fantôme ou replay de comparaison ;
   - éventuellement des objectifs optionnels conçus pour la map.

## Score et speedrun

Le speedrun doit être facilité par le produit, pas décrété comme unique manière de jouer.

Trois états sont conservés séparément :

- **Réussite** : la map est terminée, sans pression extérieure.
- **Optimisation tactique** : le nombre de commandes validées est la mesure principale.
- **Speedrun** : le temps actif est mesuré dans un mode clairement identifié.

Une soumission compétitive référence toujours :

- l’identifiant et la révision immuable de la map ;
- la version des règles ;
- le flux de commandes du replay ;
- les éventuelles options de difficulté ;
- le score et le temps calculés ;
- un hash de l’état final.

Les checkpoints restent utiles à la découverte, mais une tentative commencée depuis un checkpoint n’est pas admissible aux classements globaux.

## Contenu narratif

La narration reste légère et rentable :

- une prémisse claire ;
- des introductions courtes aux chapitres ;
- des textes ou interventions pendant les transitions ;
- de la personnalité dans les descriptions, réactions et animations ;
- aucun système de dialogues ou de cinématiques lourdes tant que le gameplay principal n’est pas validé.

Le ton existant peut être conservé, mais les textes devront être réécrits et localisés proprement.

## Périmètre retiré de la cible

Ces éléments ne sont pas portés dans la nouvelle architecture :

- comptes et mots de passe propriétaires ;
- launcher WPF ;
- serveur TCP historique ;
- API PHP historique ;
- hub social temps réel ;
- lobby, chat et présence MMO ;
- votes et scores gérés par le backend historique ;
- progression aléatoire de type récompenses quotidiennes ;
- Awakenings et leurs VFX en tant que système de gameplay ;
- incarnation et contrôle direct du familier.

Les assets visuels associés ne sont pas supprimés automatiquement. Ils peuvent être réaffectés après inventaire.

## Périmètre différé

Ne doivent pas bloquer la reconstruction du cœur :

- intégration du SDK Steam ;
- publication Workshop ;
- classements Steam ;
- Steam Cloud et succès ;
- campagne narrative finale ;
- économie complète de déblocage cosmétique ;
- production de nouvelles maps en volume ;
- éventuel service en ligne complémentaire.

## Critère de réussite du reboot

Le reboot est sur la bonne trajectoire lorsqu’une map legacy importée peut être :

1. jouée par la nouvelle simulation ;
2. annulée action par action sans divergence ;
3. rejouée depuis un flux de commandes avec le même hash final ;
4. affichée et animée sans que la présentation détienne de règles ;
5. ouverte, modifiée, validée et sauvegardée par le nouvel éditeur ;
6. comparée à son comportement attendu dans la matrice de parité.
