# Dodge From Light — matrice de parité du contenu

Cette matrice décrit le contenu à migrer et sépare trois choses :

- le comportement actuellement observé ;
- l’intention lisible dans les textes ou les maps ;
- la règle cible qui doit être figée avant implémentation.

Un comportement accidentel n’est pas automatiquement une règle.

## Inventaire mesuré

| Catalogue | Volume observé |
|---|---:|
| Types d’ennemis | 12 |
| Types de gears | 9 |
| Types de cellules | 10 |
| Maps sérialisées | 30 dossiers |
| Maps uniques | 26 identifiants |
| Entrées d’ennemis dans les maps sérialisées | 31 |
| Gears placés dans les maps sérialisées | 31 |
| Plateformes mobiles dans les maps sérialisées | 2 |
| Décors enregistrés dans `ResourcesData` | 281 |
| Entrées cosmétiques dans `SkinsData` | 1 201 |
| Familiers dans `SkinsData` | 22 |

Les quatre dossiers `Level 23` à `Level 26` à la racine des StreamingAssets partagent
leurs IDs et leur gameplay avec quatre maps du tutoriel, mais utilisent un environnement
visuel différent. L’importeur doit signaler ces quatre conflits et conserver les deux
variantes jusqu’à la décision de contenu.

## Règles communes à préserver

| Règle | Comportement actuel | Cible |
|---|---|---|
| Déclenchement d’un tour | Chaque déplacement, attente ou interaction importante avance les ennemis | Une commande acceptée produit exactement un tour |
| Mouvement | Déplacement cardinal, normalement d’une cellule | Conservé, avec prévisualisation de la destination |
| Orientation | Une rotation peut consommer un tour quand le déplacement est impossible | À rendre explicite dans l’UI et les règles |
| Détection | Entrer dans une zone de lumière tue immédiatement | Conservé |
| Patrouille | Les ennemis parcourent des segments de waypoints et dépensent des actions pour tourner | Conservé par un système de trajectoire déterministe |
| Ligne de vue | Sol, éléments, entités et miroir peuvent bloquer la vue | Conservé, avec un algorithme de grille testable |
| Deux slots de gear | Le joueur peut transporter deux gears | Conservé |
| Ramassage et utilisation | Comptent comme une action | Conservé et affiché avant confirmation |
| Défaite | Recharge actuellement toute la map | Remplacée visuellement par un restart instantané du même état initial |
| Undo | Incomplet et limité à quelques actions de l’éditeur | Undo complet dans le jeu et l’éditeur |

## Capacités du joueur hors gears

| Capacité | État actuel | Règle cible proposée |
|---|---|---|
| Attendre / tourner | Une commande sans translation peut avancer le monde | Commande explicite `Wait` et rotation explicitement prévisualisée |
| Saut sur place | Le héros quitte la grille pendant deux résolutions de tour | Conserver comme capacité de puzzle seulement si une map l’autorise ; figer précisément les dangers évités |
| Ancre d’arme | Le joueur plante son arme puis échange sa position avec elle | Conserver la mécanique, mais la découpler du skin d’arme |
| Impulsion au bouclier | Pousse l’arme plantée jusqu’à quatre cellules | Conserver comme capacité autorisable par map, découplée du skin de bouclier |
| Checkpoint | Activation manuelle ; invalide le score de la tentative | Conserver pour l’accessibilité, hors classements |
| Familier incarné | Le joueur contrôle le familier et peut transposer le héros | Supprimé |
| Familier cosmétique | Suit le héros et réagit aux événements | Conservé sans cellule logique, collision, ciblage ou effet de score |

La map cible déclare les capacités autorisées et le loadout de départ. Une tenue cosmétique ne doit jamais rendre une map soluble ou insoluble.

## Ennemis

Les occurrences comptent les 30 dossiers sérialisés, donc incluent les quatre duplications connues.

| ID cible | Ennemi | Occurrences | Comportement de puzzle à préserver | Point à figer |
|---|---|---:|---|---|
| `enemy.scout` | Brazen Scoute | 10 | Patrouille et vision en ligne droite devant lui | Figé : portée et actions viennent de la map ; tourner consomme une action |
| `enemy.cyclops` | Siamese Cyclops | 2 | Alterne vision avant et arrière selon son cycle de déplacement | Phase initiale du cycle |
| `enemy.knight` | Fiery Knight | 1 | Voit devant et sur les côtés, avec angles avant ; angle mort derrière | Forme exacte pour chaque portée supérieure à 1 |
| `enemy.lizardman` | Zioned Lizardman | 1 | Voit uniquement sur les quatre diagonales | Blocage diagonal par les obstacles |
| `enemy.keeper` | Hellish Keeper | 3 | Zone en diamant autour de lui ; généralement statique | Orientation purement visuelle ou porteuse de règle |
| `enemy.gloutton` | Thirsty Gloutton | 3 | Tue à courte portée et attire le héros situé plus loin dans son axe | Ordre attraction/détection en cas de conflit |
| `enemy.trap` | Oculte Trap | 1 | Attire puis repousse sans tuer | Remplacer les distances aléatoires par des paramètres déterministes et visibles |
| `enemy.thunderbird` | Raised Thunderbird | 1 | Alterne tous les deux tours entre deux formes de zone dangereuse | Phase initiale et interaction avec l’arrêt du temps |
| `enemy.axe` | Evil Axe | 4 | Piège statique frappant alternativement à gauche et à droite sur deux cellules | Phase initiale |
| `enemy.hammer` | Gifted Hammer | 1 | Frappe alternativement à gauche et à droite, à distance, en forme de croix | Phase initiale et occupation de la cellule centrale |
| `enemy.catapult` | Shit Catapult | 4 | Alterne recharge et impact à cinq cellules devant, avec zone en croix | Les deux cellules derrière pendant la recharge sont actuellement dangereuses ; confirmer l’intention |
| `enemy.pillar` | Pillar of Eternity | 0 | Cycle de quatre motifs : loin horizontal, loin vertical, proche horizontal, proche vertical | Absent du corpus de maps ; créer une map de caractérisation dédiée |

Chaque ennemi reçoit dans le nouveau système :

- un identifiant stable ;
- une configuration de portée, actions par tour et phase initiale ;
- un état sérialisable minimal ;
- un calcul pur de ses intentions ;
- un calcul pur de sa zone de danger ;
- des événements de présentation sans référence directe au prefab ;
- une map de caractérisation et des cas de collision.

## Gears

| ID cible | Gear | Occurrences | Comportement actuel | Cible |
|---|---|---:|---|---|
| `gear.bomb` | Bomb | 11 | Détruit les décors sur les quatre cellules cardinales adjacentes | Conserver ; prévisualiser les décors détruits |
| `gear.disco_ball` | Disco Ball | 3 | Se pose devant, dure cinq tours et étourdit le premier ennemi qui la voit | Conserver ; durée et stun configurés dans les données |
| `gear.grapple` | Grapple | 6 | Tire le héros jusqu’à cinq cellules, au-dessus des trous, arrêté par éléments et ennemis | Conserver ; destination prévisualisée |
| `gear.invisibility_cloak` | Invisibility Cloak | 0 | Positionne un booléen `Invisible`, mais la détection ne le consulte pas | Activée pendant deux tours ; bloque la détection lumineuse, pas les pièges physiques |
| `gear.mirror` | Mirror | 4 | Se pose devant, bloque mouvement et vision, puis disparaît | Durée officielle de trois tours |
| `gear.powder_of_darkness` | Powder of Darkness | 0 | `UseGear()` ne produit aucun effet | Nuage centré sur le joueur, rayon de Manhattan 2, bloque la vision pendant trois tours |
| `gear.remote_control` | Remote Control | 2 | Inverse toutes les patrouilles d’ennemis et plateformes | Conserver ; prévisualiser les nouvelles trajectoires |
| `gear.speed_boots` | Speed Boots | 3 | Donne deux cellules de déplacement pendant deux tours | Conserver ; préciser le traitement des cellules intermédiaires |
| `gear.time_controller` | Time Controller | 2 | Fige mouvements et cycles de plusieurs systèmes pendant trois tours ; les zones de vue restent actives | Conserver, mais limiter clairement la liste des systèmes affectés |

Les deux gears inachevés restent dans le contrat de contenu. Ils ne doivent pas être portés sous forme de no-op.

## Cellules et objets de plateau

| ID cible | Type actuel | Occurrences | Règle à préserver ou clarifier |
|---|---|---:|---|
| `cell.floor` | Walkable | 751 | Sol traversable |
| `cell.void` | NotWalkable | 215 | Trou ou absence de sol |
| `cell.teleporter` | TP | 3 | Téléporte vers une cellule libre aléatoire via le générateur déterministe de la partie |
| `cell.collapse` | Collapsable | 1 | S’effondre après le départ du héros |
| `cell.speed_boost` | FlipACoin | 3 | Donne deux cellules de déplacement pendant deux tours ; renommer selon sa vraie fonction |
| `cell.checkpoint` | CheckPoint | 0 | Sauvegarde de reprise locale, hors classement |
| `cell.spring` | Spring | 6 | Projette dans une direction sur une distance configurée, au-dessus des obstacles |
| `cell.timed_bomb` | Bombe | 5 | Compte à rebours, détruit sa cellule et tue le héros présent |
| `cell.needles` | Needle | 60 | Alterne actif/inactif avec une phase initiale configurable |
| `cell.sword_socket` | SwordSocle | 0 | Cellule traversable neutre avec son rendu dédié |
| `object.moving_platform` | MovingPlatform | 2 | Suit une route sur les cellules non marchables et peut transporter le héros |
| `marker.start` | StartCell | 30 | Position initiale unique |
| `marker.exit` | EndCell | 30 | Objectif de sortie unique |

### Règles de plateau déjà figées dans le Core

- Une cellule `cell.collapse` est détruite dès que le joueur la quitte. Attendre sur
  place ne la détruit pas.
- Les aiguilles changent de phase au début de chaque commande acceptée. La phase legacy
  `0` est active dans l’état initial, la phase `1` est inactive, puis elles alternent.
- Une bombe temporisée exige un compteur strictement positif, décrémente en fin de
  tour, détruit sa propre cellule à zéro et tue le joueur s’il l’occupe.
- Ces valeurs appartiennent à `BoardState` et au hash de simulation ; elles sont donc
  restaurées par undo et reproduites par replay sans état Unity caché.

## Décors et environnements

Les décors ne sont pas seulement visuels : certains occupent plusieurs cellules et bloquent mouvement ou ligne de vue. Leur migration demande un registre versionné contenant :

- identifiant legacy ;
- identifiant stable cible ;
- prefab et aperçu ;
- catégorie et tags de recherche ;
- empreinte logique sur la grille ;
- rotations autorisées ;
- échelle autorisée ;
- statut actif, masqué ou remplacé.

Les 281 entrées existantes restent importables. Les huit entrées sans catégorie doivent être classées, pas supprimées silencieusement.

## Cosmétiques

`SkinsData` contient 1 201 entrées. Le volume n’oblige pas à afficher 1 201 boutons simultanément.

La cible conserve le catalogue et ajoute :

- chargement à la demande ;
- catégories et filtres ;
- recherche ;
- favoris ;
- tenues sauvegardées ;
- bouton aléatoire contrôlé ;
- masquage éditorial d’entrées cassées sans détruire leurs IDs ;
- validation automatique des références, sockets et aperçus.

Les 22 familiers deviennent des compagnons visuels. Ils peuvent jouer des animations de déplacement, peur, victoire et réaction, mais ne font pas partie du `SimulationState`.

## Maps et format legacy

Le format actuel répartit une map entre :

- `grid.json` ;
- `metadata.json` ;
- `preview.png`.

Il sérialise notamment des valeurs d’enum numériques, des cellules complètes dupliquées pour start/end, un champ générique `Arg1` et des références de décors par nom. Ce format reste lisible par l’importeur, mais ne devient pas le format cible.

Chaque import doit :

1. lire sans modifier la source ;
2. normaliser les IDs et les coordonnées ;
3. détecter les références manquantes ;
4. convertir `Arg1` en paramètres nommés ;
5. dédupliquer les révisions identiques ;
6. produire un rapport de migration ;
7. sauvegarder une nouvelle map séparée ;
8. vérifier le hash et la relecture.

## Décisions de règles appliquées

1. L’Invisibility Cloak bloque la détection lumineuse pendant deux tours. Les pièges
   physiques et les collisions restent actifs.
2. La Powder of Darkness crée pendant trois tours un nuage de rayon de Manhattan 2
   centré sur le joueur. Il bloque la vision et pas les déplacements.
3. Les téléporteurs conservent une destination libre aléatoire. Le tirage appartient à
   l’état de simulation et se reproduit à l’identique dans un replay.
4. L’Oculte Trap conserve les quatre bornes de distance de la map et utilise le même
   générateur déterministe.
5. Le Mirror dure officiellement trois tours.
6. Le saut est une commande explicite qui protège de la vision ennemie pendant sa
   résolution. Les autres dangers restent pilotés par leurs règles propres.
7. L’ancre d’arme et son impulsion sont des capacités de base, découplées des skins.
8. Le Sword Socle est une cellule traversable neutre.
9. Les acteurs sont résolus dans l’ordre stable de leurs définitions. Un acteur bloqué
   inverse sa route.
10. Une commande résout l’entrée de cellule, les plateformes, les ennemis, les dangers
    de fin de tour, puis la victoire.
11. Les ressorts s’enchaînent automatiquement. Une cellule spéciale déjà visitée arrête
    la chaîne pour empêcher toute boucle infinie.
