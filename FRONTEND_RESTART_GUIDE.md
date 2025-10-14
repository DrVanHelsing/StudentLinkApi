# Frontend Restart and Cache Clear Script

## IMPORTANT: Follow these steps to see your UI changes

### Option 1: Quick Browser Cache Clear (Fastest)
1. Open your browser to `http://localhost:3000`
2. Press `Ctrl + Shift + R` (Windows/Linux) or `Cmd + Shift + R` (Mac)
3. This performs a hard refresh and clears the cache

### Option 2: Full Cache Clear
1. Open your browser Developer Tools (F12)
2. Right-click the refresh button
3. Select "Empty Cache and Hard Reload"

### Option 3: Restart Dev Server (If above doesn't work)

**PowerShell Commands:**
```powershell
# Navigate to frontend directory
cd "C:\MAUI Applications\StudentLinkApi_Sln\studentlink-frontend"

# Clear npm cache
npm cache clean --force

# Remove node_modules and package-lock (only if really needed)
# Remove-Item -Recurse -Force node_modules
# Remove-Item package-lock.json

# Reinstall (only if you removed node_modules)
# npm install

# Start dev server
npm start
```

### Option 4: Manual File Check

Verify the files were actually saved with the changes:

**PowerShell Command to check icons.js:**
```powershell
Get-Content "C:\MAUI Applications\StudentLinkApi_Sln\studentlink-frontend\src\utils\icons.js" | Select-String -Pattern "apply|active"
```

**PowerShell Command to check DashboardPage.js:**
```powershell
Get-Content "C:\MAUI Applications\StudentLinkApi_Sln\studentlink-frontend\src\pages\DashboardPage.js" | Select-String -Pattern "ICONS.document|ICONS.job"
```

### Common Issues and Solutions

#### Issue: Changes not showing
**Solution:** 
- Hard refresh browser (Ctrl + Shift + R)
- Check browser console for errors (F12)
- Verify files saved correctly

#### Issue: Module not found errors
**Solution:**
```powershell
cd "C:\MAUI Applications\StudentLinkApi_Sln\studentlink-frontend"
npm install
```

#### Issue: Port 3000 already in use
**Solution:**
```powershell
# Find and kill the process
Get-Process -Name "node" | Stop-Process -Force

# Then restart
npm start
```

### Verification Checklist

After restarting, check these pages:
- [ ] Dashboard - Recent activity should show ??, ??, ??
- [ ] Jobs page - Search should show ??, location ??
- [ ] CV Upload - Should show ??, ?? icons
- [ ] Progress Dashboard - Should show ??, ??, ?
- [ ] Interactive Feedback - Should show section icons

### Quick Test

Open browser console (F12) and run:
```javascript
// This should return an object with all icons
import('../utils/icons.js').then(icons => console.log(icons.default));
```

---

## Most Likely Solution

**Just press `Ctrl + Shift + R` in your browser!**

The files are already saved and the build succeeded. Your browser is just showing a cached version.
