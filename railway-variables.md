# 🔐 Configuration des Variables d'Environnement Railway

## 📋 Variables à configurer dans Railway

### 1. Variables d'environnement principales

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
```

### 2. Variables MySQL (générées automatiquement par Railway)

```env
MYSQL_HOST=mainline.proxy.rlwy.net
MYSQL_DATABASE=railway
MYSQL_USER=root
MYSQL_PASSWORD=WCrDOYlSIPdNJriAlsizKEUSyCXmZRvE
```

## 🔧 Comment configurer dans Railway

### Étape 1 : Accéder aux variables d'environnement
1. Allez dans votre projet Railway
2. Cliquez sur votre service d'application
3. Allez dans l'onglet **"Variables"**

### Étape 2 : Ajouter les variables
1. Cliquez sur **"New Variable"**
2. Ajoutez chaque variable une par une :

| Variable | Valeur |
|----------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://0.0.0.0:$PORT` |
| `MYSQL_HOST` | `mainline.proxy.rlwy.net` |
| `MYSQL_DATABASE` | `railway` |
| `MYSQL_USER` | `root` |
| `MYSQL_PASSWORD` | `WCrDOYlSIPdNJriAlsizKEUSyCXmZRvE` |

### Étape 3 : Redéployer
1. Après avoir ajouté toutes les variables
2. Railway redéploiera automatiquement
3. L'application utilisera les variables d'environnement

## 🔒 Sécurité

### ✅ Bonnes pratiques
- ✅ Variables d'environnement dans Railway
- ✅ Pas de mots de passe dans le code
- ✅ Fichiers sensibles dans .gitignore
- ✅ Configuration de production séparée

### ❌ À éviter
- ❌ Commiter les mots de passe
- ❌ Hardcoder les chaînes de connexion
- ❌ Exposer les variables sensibles

## 🧪 Test de la configuration

### Vérifier les variables
```bash
# Dans Railway, vérifiez que toutes les variables sont présentes
echo $MYSQL_HOST
echo $MYSQL_DATABASE
echo $MYSQL_USER
```

### Tester la connexion
L'application devrait se connecter automatiquement à MySQL et appliquer les migrations.

## 📞 Support

En cas de problème :
1. Vérifiez que toutes les variables sont configurées
2. Consultez les logs Railway
3. Testez la connexion MySQL
4. Vérifiez les permissions de la base de données
