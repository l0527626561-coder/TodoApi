# Todo List - Fullstack Application

אפליקציית Todo List Fullstack עם .NET Minimal API ו-React.

## דרישות מקדימות

- **.NET 8.0** או גבוה יותר
- **Node.js** (v16 או גבוה יותר)
- **MySQL** (מותקן ורץ)
- **MySQL Workbench** (אופציונלי, לניהול DB)

## הגדרה ראשונית

### 1. הגדרת MySQL Database

1. פתח את MySQL Workbench
2. התחבר לשרת המקומי
3. צור schema חדש בשם `ToDoDB`
4. הרץ את ה-SQL הבא:

```sql
CREATE TABLE Users (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Username VARCHAR(50) NOT NULL UNIQUE,
  PasswordHash VARCHAR(255) NOT NULL
);

CREATE TABLE Items (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Name VARCHAR(100) NOT NULL,
  IsComplete TINYINT(1) DEFAULT 0,
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  UserId INT NOT NULL,
  FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

### 2. הגדרת Backend (.NET)

1. עדכן את `appsettings.json` עם פרטי ה-MySQL שלך:

```json
"ConnectionStrings": {
  "ToDoDB": "server=localhost;user=root;password=YOUR_PASSWORD;database=ToDoDB"
}
```

2. התקן את ה-packages:

```bash
dotnet restore
```

3. הרץ את ה-API:

```bash
dotnet watch run
```

ה-API יהיה זמין ב-`http://localhost:8080`

### 3. הגדרת Frontend (React)

1. נווט לתיקיית ה-React:

```bash
cd ToDoListReact-master
```

2. התקן את ה-dependencies:

```bash
npm install
```

3. הרץ את ה-React app:

```bash
npm start
```

ה-App יפתח ב-`http://localhost:3000`

## תכונות

### Authentication
- **Registration**: הרשמה למשתמש חדש
- **Login**: התחברות למשתמש קיים
- **JWT Tokens**: אבטחה עם JWT tokens
- **Auto Logout**: התנתקות אוטומטית ב-401 errors

### Todo Management
- **Create**: הוספת משימה חדשה
- **Read**: הצגת כל המשימות של המשתמש
- **Update**: עדכון משימה (שם וסטטוס)
- **Delete**: מחיקת משימה

## API Endpoints

### Authentication
- `POST /api/auth/register` - הרשמה
- `POST /api/auth/login` - התחברות
- `GET /api/auth/me` - קבלת פרטי המשתמש הנוכחי

### Items
- `GET /api/items` - קבלת כל המשימות
- `GET /api/items/{id}` - קבלת משימה ספציפית
- `POST /api/items` - יצירת משימה חדשה
- `PUT /api/items/{id}` - עדכון משימה
- `DELETE /api/items/{id}` - מחיקת משימה

## Swagger Documentation

כאשר ה-API רץ, אתה יכול לגשת ל-Swagger UI ב-`http://localhost:8080/swagger`

## Troubleshooting

### MySQL Connection Error
- וודא ש-MySQL רץ
- בדוק את פרטי ה-connection string ב-`appsettings.json`
- וודא שה-database `ToDoDB` קיים

### CORS Error
- ה-CORS כבר מוגדר לאפשר כל מקור
- אם עדיין יש בעיות, בדוק את ה-browser console

### JWT Token Issues
- וודא ש-token שמור ב-localStorage
- בדוק את ה-token expiration time ב-`appsettings.json`

## Development Notes

- ה-API משתמש ב-in-memory database ב-development (אם MySQL לא מוגדר)
- כל משימה קשורה למשתמש ספציפי
- Tokens מתפוגים אחרי 120 דקות (ניתן לשנות ב-`appsettings.json`)
