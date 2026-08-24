# 🌐 NexPOS API - Render.com Hosting Guide (Step-by-Step)

> **Goal**: Deploy your NexPOS Backend API to Render.com  
> **Final URL Format**: `https://nexpos-api-YOUR_UNIQUE.onrender.com`  
> **Method**: Docker Container (because .NET 10 Preview = Render Native Runtime Not Available Yet)  
> **Time needed**: ~15 minutes  
> **Estimated Cost**: Free tier for 750 hours/month (1 instance) OR $7/month Standard Plan

---

## 📋 Prerequisites Checklist

| Requirement | Status | Instructions |
|------------|--------|-------------|
| ✅ GitHub Account | ⬜ | Create free: https://github.com/signup |
| ✅ Render.com Account | ⬜ | Sign up FREE: https://dashboard.render.com/register (Use GitHub Login) |
| ✅ MongoDB Atlas Cluster | ✅ **You Already Have This!** | Check `appsettings.json`: `cluster0.tz5au5t.mongodb.net` |
| ✅ Source Code Pushed to GitHub | ⬜ | See Step 1 below |
| ✅ Dockerfile + .dockerignore + render.yaml | ✅ **Already Created!** | Files added to your project root |

---

## 📁 Files Created for Your Deployment

Your project folder `d:\Learning\Pos_SD5\` এ নতুন যে ফাইলগুলো তৈরি করা হয়েছে:

| File | Purpose |
|------|---------|
| `Dockerfile` | 2-stage Docker build for Render (SDK build → ASPNET Runtime) |
| `.dockerignore` | Optimizes build (skips Desktop WPF project, bin/obj, docs etc.) |
| `render.yaml` | Blueprint file = **One-click deploy** |
| `src/POS.API/appsettings.Production.json` | Production config defaults (Env Vars override this) |

---

# 🚀 DEPLOYMENT STEPS (Follow Exactly in Order!)

---

## STEP 1️⃣: Push Your Code to GitHub (CRITICAL - Do this FIRST)

Render needs your code to live on GitHub/GitLab.

### On Windows (PowerShell/Terminal):

```powershell
cd d:\Learning\Pos_SD5

# 1.1 Initialize Git (if not done already)
git init

# 1.2 Add a .gitignore (if doesn't exist)
```

👉 **Create file**: `d:\Learning\Pos_SD5\.gitignore` (আগে না থাকলে)
```
bin/
obj/
*.user
*.suo
.vs/
.vscode/
publish/
*.dll
*.pdb
*.exe
```

Continue in terminal:
```powershell
# 1.3 Add all files
git add -A

# 1.4 Commit locally
git commit -m "Initial commit: NexPOS Enterprise with Docker + Render config"

# 1.5 Go to GitHub.com → New Repository (name: Pos_SD5, set to PUBLIC)
#     Example URL: https://github.com/YOUR_USERNAME/Pos_SD5

# 1.6 Link your local repo to GitHub (Replace YOUR_USERNAME!)
git remote add origin https://github.com/YOUR_USERNAME/Pos_SD5.git
git branch -M main
git push -u origin main
```

✅ **Verify**: ব্রাউজারে আপনার GitHub Repo URL-এ গিয়ে `Dockerfile` দেখা যাচ্ছে কিনা চেক করুন।

---

## STEP 2️⃣: Deploy to Render (Two Methods - Pick One)

---

### ⭐ METHOD A: Blueprint (One-Click via render.yaml)

Easiest method - Render reads your `render.yaml` file automatically.

2.1 Go to: https://dashboard.render.com/blueprints  
2.2 Click **"New Blueprint Instance"**  
2.3 Connect your GitHub account and select your `Pos_SD5` repo  
2.4 Branch: **main**  
2.5 Service Name: `nexpos-api` (or anything unique)  
2.6 Click **"Apply"**

✅ Blueprint will auto-create:
- Runtime: Docker
- Plan: Standard
- Environment Variables (from render.yaml)
- Health Check Path: `/` (Swagger UI)

---

### 🔧 METHOD B: Manual Web Service Creation (More Control)

2.1 Go to Render Dashboard: https://dashboard.render.com/  
2.2 Click **"New +"** → Select **"Web Service"**  
2.3 Connect Repository: Choose your GitHub → `Pos_SD5` repo  
2.4 Configure:

| Field | Value (EXACTLY!) |
|-------|-----------------|
| **Name** | `nexpos-api` (must be unique - Render will assign free subdomain) |
| **Region** | **Singapore** (Nearest to Bangladesh = Lowest Latency 🇸🇬) |
| **Branch** | `main` |
| **Runtime** | **Docker** ⚠️ (NOT .NET - pick Docker from dropdown!) |
| **Root Directory** | *(leave empty)* |
| **Dockerfile Path** | `./Dockerfile` |
| **Docker Build Context** | `.` |
| **Plan** | Free (for testing) or Standard ($7/mo for 24/7 - Free sleeps after 15 mins idle) |

2.5 Click **"Create Web Service"**  
2.6 Now wait - the build will fail initially because we need to set Environment Variables.

---

## STEP 3️⃣: Set Environment Variables (🔒 IMPORTANT SECURITY)

✅ **Do this before or right after first build**

On Render Dashboard → `nexpos-api` → **Environment** Tab → Click **"Add Environment Variable"**:

> **Note**: .NET uses `__` (double underscore) for nested config like `MongoDbSettings__ConnectionString` (= appsettings.json → `MongoDbSettings.ConnectionString`)

| KEY (Exact!) | VALUE | Sync |
|--------------|-------|------|
| **`MongoDbSettings__ConnectionString`** | `mongodb+srv://gmrenco4_db_user:G02m6FsVR8njgNgY@cluster0.tz5au5t.mongodb.net/` | ❌ No Sync |
| **`MongoDbSettings__DatabaseName`** | `POS_SD5_Production` (Use this to keep Production DB separate from Local) | ✅ Yes |
| **`JwtSettings__SecretKey`** | Click **"Generate"** Button OR type: A very long random string (32+ chars, Example: `a1B2c3D4e5F6g7H8i9J0k1L2m3N4o5P6q7R8s9T0u1V2w3X4y5Z6`) | ❌ No Sync (🔐 Secret!) |
| **`JwtSettings__Issuer`** | `NexPosAPI` | ✅ Yes |
| **`JwtSettings__Audience`** | `NexPosClient` | ✅ Yes |
| **`JwtSettings__ExpiryMinutes`** | `1440` (= 24 hours - increase/decrease if you want) | ✅ Yes |
| **`ASPNETCORE_ENVIRONMENT`** | `Production` | ✅ Yes |

After adding all 7 variables: Click **"Save Changes"** → Render auto-restarts.

---

## STEP 4️⃣: Verify Build Logs & Deploy

📊 Go to Render → **"Events"** Tab (left sidebar) for your service:

### What a Successful Build Looks Like:
```
==> Cloning repository...
==> Detected service type: docker
==> Building Dockerfile: ./Dockerfile
  → Stage 1 (Build): dotnet restore → dotnet publish...
  → Stage 2 (Runtime): ASPNET Core 10 Preview image
==> Build succeeded 🎉
==> Starting service...
==> Service is live! 🟢
  → 🔗 https://nexpos-api-XXXX.onrender.com
```

⏰ **Wait time**: Docker build takes 3-8 minutes (first time - because of NuGet restore)

❌ **If build fails**: Click the failed event → Scroll to see error → Send me the error screenshot.

---

## STEP 5️⃣: Test Your Live API! 🎯

Once Render shows **"Live 🟢"** status:

### Open Browser → Visit your Deployed URL:
```
https://nexpos-api-YOUR_UNIQUE.onrender.com/
```

✅ **SUCCESS = Swagger UI Loads!** (সব endpoints দেখা যাবে)
You'll see: "NexPOS Point of Sale API v1" page with all GET/POST/PUT/DELETE endpoints.

### Quick API Test (Swagger UI):
5.1 Expand **`POST /api/auth/login`**  
5.2 Click **"Try it out"** → Paste this JSON:
```json
{
  "username": "admin",
  "password": "ChangeMe123!"
}
```
5.3 Click **"Execute"**  
✅ **Response Code 200** + a Token in `data.token` field = ✅ Database Connection Works + API Functional!

---

## STEP 6️⃣: Update WPF Desktop App to Use Your LIVE API (Last Step!)

এখন আপনার Desktop App-কে Render-এ হোস্ট করা API ব্যবহার করতে বলুন।

### 📝 Edit File: `d:\Learning\Pos_SD5\src\POS.Desktop\App.xaml.cs`

👉 **Line 52**: Change `http://localhost:5000/` to YOUR Render URL:

```csharp
// 🔧 OLD (Localhost - Line 52):
services.AddSingleton(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5000/"),   // ⚠️ CHANGE THIS!
    Timeout = TimeSpan.FromSeconds(30)
});

// ✅ NEW (Render LIVE - Replace with YOUR URL!):
services.AddSingleton(sp => new HttpClient
{
    BaseAddress = new Uri("https://nexpos-api-YOUR_UNIQUE.onrender.com/"),  // 👈 YOUR RENDER URL
    Timeout = TimeSpan.FromSeconds(60)  // Increase timeout for Render cold start (20→60s)
});
```

### Rebuild Desktop:
```powershell
cd d:\Learning\Pos_SD5
dotnet build src/POS.Desktop/POS.Desktop.csproj -c Release
```

### ✅ TEST Desktop + LIVE API:
```powershell
cd d:\Learning\Pos_SD5\src\POS.Desktop
dotnet run -c Release
```

Login with `admin / ChangeMe123!` → Dashboard loads from Render-hosted API! 🎉

---

## 💰 Pricing: Which Render Plan?

| Plan | Price | Uptime | RAM | CPU | Recommended For |
|------|-------|--------|-----|-----|-----------------|
| **Free** | $0 | 750 hrs/month = **15m idle sleep** ❌ | 512MB | 0.1x | **Testing Only** |
| **Starter** | $1.74/mo | 24/7 ✅ No Sleep | 1GB | 0.5x | Hobby / 1 cashier |
| **Standard** ⭐ | $7/mo | 24/7 ✅ No Sleep | 2GB | 1x | **Production - 1-5 cashiers** |
| **Pro 1** | $22/mo | 24/7 ✅ | 4GB | 2x | Busy Store / 5+ users |

💡 **Tips for Free Tier Users**:
- First user opens app = 30-40s cold start delay (Render wakes up)
- After 15 mins no API calls = Goes to sleep
- Use Standard ($7) for production business usage

---

## 🌐 Desktop Distribution (Share with Cashiers!)

আপনি Render-এ API Live করলে, আপনি Desktop .exe build করে যেকোনো Windows PC-এ ইনস্টল করতে পারবেন:

### Build Standalone EXE:
```powershell
cd d:\Learning\Pos_SD5\src\POS.Desktop

dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o "d:\Learning\Pos_SD5\publish\LIVE-Version"
```

✅ Output folder: `d:\Learning\Pos_SD5\publish\LIVE-Version\POS.Desktop.exe`  
যেকোনো Windows 10/11 PC-এ পাঠিয়ে দিন - কোনো .NET Runtime লাগবে না! (Self-contained)

---

## 🚨 Troubleshooting Common Errors

| Error | Cause | Fix |
|-------|-------|-----|
| **Docker Build Fails:** `Framework '10.0.0' not found` | Typo in Docker tag? | Check Dockerfile: uses `mcr.microsoft.com/dotnet/sdk:10.0-preview` ✅ Correct |
| **Swagger loads but Login = 500 Error** | MongoDB connection string wrong / Atlas IP Whitelist | Go to MongoDB Atlas → Network Access → Add IP: `0.0.0.0/0` (Allow All for Render) |
| **401 Unauthorized on all endpoints** | JWT Key mismatch between Deploy and Local | Make sure `JwtSettings__SecretKey` env var is set & non-empty |
| **Desktop says "Connection Error"** | Wrong BaseAddress in `App.xaml.cs` | Double-check Render URL: no trailing `/` issues, use HTTPS |
| **Render = "Application failed to respond"** | Port not 8080 in Dockerfile | Check Dockerfile: ENV `ASPNETCORE_URLS=http://+:8080` ✅ Correct |
| **Slow first request (30s+)** | Free tier cold start | Upgrade to Standard Plan ($7/mo) = No cold starts |

---

## 🔐 MongoDB Atlas - IP Whitelist for Render (IMPORTANT!)

Render IP addresses = Dynamic (no fixed IP). So MongoDB Atlas-এ Allow All করুন:

1. Go to: https://cloud.mongodb.com → Your Cluster `Cluster0`
2. Left Sidebar → **Network Access**
3. Click: **"+ ADD IP ADDRESS"**
4. In **"Access List Entry"** → Type: `0.0.0.0/0` (Includes 0.0.0.0/0)
5. Comment: "Render Cloud Hosting"
6. Check: ✅ **Include Atlas UI access from anywhere**
7. Click: **Confirm**

⏰ Allow few minutes for MongoDB Atlas to propagate IP rules.

---

## ✅ Final Production Checklist

| Check | Status |
|-------|--------|
| Code pushed to GitHub (Public/Private) | ⬜ |
| Docker Build = Success on Render | ⬜ |
| Service = LIVE status (Green Dot) | ⬜ |
| Swagger UI opens in Browser | ⬜ |
| /api/auth/login = Returns JWT (admin/ChangeMe123!) | ⬜ |
| MongoDB Atlas IP 0.0.0.0/0 = Added to Whitelist | ⬜ |
| Environment Variables set (all 7) | ⬜ |
| JWT SecretKey = Strong Random 32+ chars | ⬜ |
| Separate Production DB: `POS_SD5_Production` | ⬜ |
| App.xaml.cs = Line 52 updated → Render URL | ⬜ |
| Desktop Login Works with Live API | ⬜ |
| Rebuild & Distribute Live-Version EXE | ⬜ |

---

## 🎉 CONGRATULATIONS! You're Live!

আপনার NexPOS API এখন world-wide access এ আছে:
```
🌍 Backend (API):  https://nexpos-api-YOUR.onrender.com/
👨‍💼 WPF Desktop:   Uses LIVE Cloud DB
📱 Mobile/Web:     Future = Same API URL for Android/iOS/Web (Add new frontend)
🗄️ Database:      MongoDB Atlas Cloud (Auto backups, 99.999% Uptime)
💻 Scalability:   1-click Scale Up/Down in Render Dashboard
```

---

*📘 Guide Created: 21-Aug-2026 for NexPOS Enterprise SD5*  
*🔧 Need Help? Contact the Dev team!*
