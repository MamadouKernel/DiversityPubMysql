# 🚀 Guide de Déploiement Railway - DiversityPub MySQL

## 📋 Prérequis

1. **Compte Railway** : [railway.app](https://railway.app)
2. **Repository GitHub** : Votre projet doit être sur GitHub
3. **Base de données MySQL** : Railway propose MySQL

## 🔧 Étapes de Déploiement

## 🐳 Options de Build

### Option 1 : Dockerfile (Recommandé)
- **Fichier** : `Dockerfile`
- **Avantages** : Contrôle total, optimisé, multi-stage build
- **Utilisation** : Railway détecte automatiquement le Dockerfile

### Option 2 : Nixpacks
- **Fichier** : `nixpacks.toml`
- **Avantages** : Simple, automatique, optimisé pour Railway
- **Utilisation** : Supprimez Dockerfile, gardez nixpacks.toml

### Option 3 : Build Automatique
- **Fichiers** : Aucun fichier spécial
- **Avantages** : Très simple, Railway fait tout
- **Utilisation** : Supprimez Dockerfile et nixpacks.toml

### Étape 1 : Créer un projet Railway

1. Connectez-vous à [Railway](https://railway.app)
2. Cliquez sur **"New Project"**
3. Sélectionnez **"Deploy from GitHub repo"**
4. Choisissez votre repository `DiversityPubMysql`

### Étape 2 : Configurer la Base de Données MySQL

1. Dans votre projet Railway, cliquez sur **"New Service"**
2. Sélectionnez **"Database"** → **"MySQL"**
3. Notez les variables d'environnement générées :
   - `MYSQL_HOST`
   - `MYSQL_DATABASE`
   - `MYSQL_USER`
   - `MYSQL_PASSWORD`

### Étape 3 : Configurer l'Application

1. Retournez à votre service d'application
2. Allez dans l'onglet **"Variables"**
3. Ajoutez les variables d'environnement suivantes :

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
MYSQL_HOST=${MYSQL_HOST}
MYSQL_DATABASE=${MYSQL_DATABASE}
MYSQL_USER=${MYSQL_USER}
MYSQL_PASSWORD=${MYSQL_PASSWORD}
```

### Étape 4 : Déployer l'Application

**Option 1 : Avec Dockerfile (Recommandé)**
1. Railway détectera automatiquement le `Dockerfile`
2. Le build se lancera automatiquement
3. L'application sera déployée sur l'URL fournie par Railway

**Option 2 : Avec Nixpacks**
1. Supprimez le `Dockerfile` et gardez `nixpacks.toml`
2. Railway utilisera Nixpacks pour le build
3. L'application sera déployée automatiquement

**Option 3 : Build automatique**
1. Supprimez `Dockerfile` et `nixpacks.toml`
2. Railway détectera automatiquement que c'est un projet .NET
3. Le build se fera automatiquement

## 🗄️ Configuration de la Base de Données

### Migration Automatique

L'application appliquera automatiquement les migrations au démarrage grâce à :

```csharp
// Dans Program.cs
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<DiversityPubDbContext>();
        context.Database.Migrate();
    }
}
```

### Variables d'Environnement MySQL

Railway génère automatiquement ces variables :
- `MYSQL_HOST` : Host de la base de données
- `MYSQL_DATABASE` : Nom de la base de données
- `MYSQL_USER` : Nom d'utilisateur
- `MYSQL_PASSWORD` : Mot de passe

## 🔍 Vérification du Déploiement

### 1. Vérifier les Logs
- Allez dans l'onglet **"Deployments"**
- Cliquez sur le dernier déploiement
- Vérifiez les logs pour les erreurs

### 2. Tester l'Application
- Cliquez sur l'URL fournie par Railway
- Vérifiez que l'application démarre
- Testez la connexion à la base de données

### 3. Vérifier la Base de Données
- Connectez-vous à MySQL via Railway
- Vérifiez que les tables sont créées

## 🛠️ Dépannage

### Erreur de Connexion MySQL
```
Unable to connect to any of the specified MySQL hosts
```
**Solution :** Vérifiez les variables d'environnement MySQL

### Erreur de Migration
```
Table already exists
```
**Solution :** Supprimez les migrations existantes et recréez-les

### Erreur de Port
```
Failed to bind to address
```
**Solution :** Vérifiez que `ASPNETCORE_URLS` est configuré correctement

## 📈 Monitoring et Maintenance

### Logs en Temps Réel
- Railway fournit des logs en temps réel
- Surveillez les erreurs de base de données
- Vérifiez les performances

### Sauvegarde de la Base de Données
- Railway propose des sauvegardes automatiques
- Configurez des sauvegardes manuelles si nécessaire

### Mise à Jour
- Poussez les modifications sur GitHub
- Railway redéploiera automatiquement
- Les migrations s'appliqueront automatiquement

## 🔒 Sécurité

### Variables d'Environnement
- Ne committez jamais les mots de passe
- Utilisez les variables d'environnement Railway
- Activez le chiffrement SSL pour MySQL

### HTTPS
- Railway fournit automatiquement HTTPS
- Configurez les redirections HTTP vers HTTPS

## 📊 Performance

### Optimisations Recommandées
1. **Connection Pooling** : Configuré automatiquement
2. **Caching** : Implémentez Redis si nécessaire
3. **CDN** : Utilisez un CDN pour les assets statiques

### Monitoring
- Surveillez l'utilisation CPU/Mémoire
- Configurez des alertes
- Optimisez les requêtes de base de données

## ✅ Checklist de Déploiement

- [ ] Projet Railway créé
- [ ] Base de données MySQL configurée
- [ ] Variables d'environnement définies
- [ ] Application déployée avec succès
- [ ] Migrations appliquées
- [ ] Tests de connexion effectués
- [ ] HTTPS configuré
- [ ] Monitoring activé
- [ ] Sauvegardes configurées

## 🆘 Support

En cas de problème :
1. Vérifiez les logs Railway
2. Testez la connexion MySQL
3. Vérifiez les variables d'environnement
4. Consultez la documentation Railway
5. Contactez le support Railway si nécessaire

## 🔗 Liens Utiles

- [Documentation Railway](https://docs.railway.app/)
- [Guide .NET sur Railway](https://docs.railway.app/deploy/deployments/dockerfile)
- [Configuration MySQL](https://docs.railway.app/databases/mysql)
- [Variables d'environnement](https://docs.railway.app/deploy/environment-variables)
