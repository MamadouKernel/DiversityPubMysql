# 🗄️ Migration vers MySQL - Guide Complet

## 📋 Prérequis

### 1. Installation de MySQL Server
- Téléchargez MySQL Server depuis : https://dev.mysql.com/downloads/mysql/
- Installez MySQL Server avec les options par défaut
- Notez le mot de passe root que vous définissez

### 2. Outils MySQL (optionnel mais recommandé)
- MySQL Workbench : https://dev.mysql.com/downloads/workbench/
- Ou phpMyAdmin si vous préférez une interface web

## 🔧 Configuration

### Étape 1 : Exécuter le script de configuration
```powershell
.\setup_mysql.ps1
```

Ce script va :
- Vérifier que MySQL est installé
- Créer la base de données `DB_Diversity`
- Mettre à jour `appsettings.json` avec la nouvelle chaîne de connexion

### Étape 2 : Supprimer les anciennes migrations SQL Server
```powershell
Remove-Item Migrations\*.cs
```

### Étape 3 : Créer une nouvelle migration MySQL
```powershell
dotnet ef migrations add InitialCreate
```

### Étape 4 : Appliquer la migration
```powershell
dotnet ef database update
```

## 🔍 Vérification

### Tester la connexion
```powershell
dotnet run
```

L'application devrait démarrer sans erreur de base de données.

### Vérifier les tables dans MySQL
```sql
USE DB_Diversity;
SHOW TABLES;
```

## ⚠️ Différences importantes entre SQL Server et MySQL

### 1. Types de données
- `NVARCHAR` → `VARCHAR` avec `utf8mb4`
- `DATETIME2` → `DATETIME`
- `UNIQUEIDENTIFIER` → `CHAR(36)`

### 2. Fonctions SQL
- `GETDATE()` → `NOW()`
- `ISNULL()` → `IFNULL()`
- `LEN()` → `LENGTH()`

### 3. Pagination
- SQL Server : `OFFSET ... FETCH`
- MySQL : `LIMIT ... OFFSET`

## 🛠️ Dépannage

### Erreur de connexion
```
Unable to connect to any of the specified MySQL hosts
```
**Solution :** Vérifiez que le service MySQL est démarré

### Erreur d'authentification
```
Access denied for user 'root'@'localhost'
```
**Solution :** Vérifiez le mot de passe dans `appsettings.json`

### Erreur de caractères
```
Incorrect string value
```
**Solution :** Assurez-vous que la base de données utilise `utf8mb4`

## 📊 Migration des données existantes

Si vous avez des données dans SQL Server à migrer :

### 1. Exporter depuis SQL Server
```sql
-- Dans SQL Server Management Studio
SELECT * FROM TableName
-- Exporter en CSV ou SQL
```

### 2. Importer dans MySQL
```sql
-- Dans MySQL
LOAD DATA INFILE 'data.csv' 
INTO TABLE TableName 
FIELDS TERMINATED BY ',' 
LINES TERMINATED BY '\n';
```

## 🔒 Sécurité

### Configuration recommandée pour la production
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DB_Diversity;User=diversity_user;Password=strong_password;CharSet=utf8mb4;SslMode=Required;"
  }
}
```

### Créer un utilisateur dédié
```sql
CREATE USER 'diversity_user'@'localhost' IDENTIFIED BY 'strong_password';
GRANT ALL PRIVILEGES ON DB_Diversity.* TO 'diversity_user'@'localhost';
FLUSH PRIVILEGES;
```

## 📈 Performance

### Optimisations MySQL recommandées
```sql
-- Ajouter des index sur les colonnes fréquemment utilisées
CREATE INDEX idx_agent_terrain_user_id ON AgentsTerrain(UtilisateurId);
CREATE INDEX idx_activation_campagne_id ON Activations(CampagneId);

-- Optimiser les requêtes avec EXPLAIN
EXPLAIN SELECT * FROM AgentsTerrain WHERE UtilisateurId = 1;
```

## ✅ Checklist de migration

- [ ] MySQL Server installé et configuré
- [ ] Script `setup_mysql.ps1` exécuté avec succès
- [ ] Anciennes migrations supprimées
- [ ] Nouvelle migration MySQL créée
- [ ] Migration appliquée à la base de données
- [ ] Application testée et fonctionnelle
- [ ] Données migrées (si applicable)
- [ ] Sauvegarde de l'ancienne base SQL Server

## 🆘 Support

En cas de problème :
1. Vérifiez les logs de l'application
2. Consultez les logs MySQL : `C:\ProgramData\MySQL\MySQL Server 8.0\Data\`
3. Testez la connexion avec MySQL Workbench
4. Vérifiez que tous les packages NuGet sont à jour
