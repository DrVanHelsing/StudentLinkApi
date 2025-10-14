# ?? StudentLink Frontend - Complete Guide

## ?? Quick Start (5 minutes)

### Prerequisites
- ? Node.js 16+ (Download: https://nodejs.org/)
- ? Your API running on `https://localhost:7068`

### Setup & Run

```powershell
# 1. Setup frontend (one-time)
cd infrastructure
.\setup-frontend.ps1

# 2. Start frontend
cd ..\studentlink-frontend
npm start

# Frontend will open at: http://localhost:3000
```

---

## ?? What's Included

### Pages
- **Home Page** - Landing page with sign in/up buttons
- **Login Page** - User authentication
- **Register Page** - New user registration
- **Dashboard** - Role-based dashboards (Student/Recruiter/Admin)
- **Profile Page** - View and edit user profile

### Features
- ? **JWT Authentication** - Secure token-based auth
- ? **Protected Routes** - Route guards based on authentication
- ? **Role-Based UI** - Different dashboards for each role
- ? **Profile Management** - Update user information
- ? **Responsive Design** - Works on all devices
- ? **Modern UI** - Tailwind CSS styling
- ? **Error Handling** - User-friendly error messages

---

## ?? User Flows

### Student Flow
1. Register/Login as Student
2. View Student Dashboard with:
   - Profile completeness
   - Applications count
   - Messages
   - Quick actions (Upload CV, Browse Jobs, etc.)
3. Edit profile information
4. View recent activities

### Recruiter Flow
1. Register/Login as Recruiter
2. View Recruiter Dashboard with:
   - Active jobs
   - Applications received
   - Candidate pool
   - Interview schedule
3. Quick actions (Post Job, Search Candidates, etc.)
4. Manage job postings

### Admin Flow
1. Register/Login as Admin
2. View Admin Dashboard with:
   - Total users
   - Active jobs
   - Applications
   - System revenue
3. Admin tools (Manage Users, Settings, Analytics)
4. System status monitoring

---

## ??? Technology Stack

| Technology | Purpose |
|-----------|---------|
| **React 18** | UI Framework |
| **React Router** | Navigation & Routing |
| **Axios** | HTTP Client |
| **Tailwind CSS** | Styling |
| **Context API** | State Management |

---

## ?? Project Structure

```
studentlink-frontend/
??? public/
?   ??? index.html              # HTML template
??? src/
?   ??? components/
?   ?   ??? Navbar.js          # Navigation bar
?   ?   ??? ProtectedRoute.js  # Route protection
?   ??? contexts/
?   ?   ??? AuthContext.js     # Authentication state
?   ??? pages/
?   ?   ??? LoginPage.js       # Login form
?   ?   ??? RegisterPage.js    # Registration form
?   ?   ??? DashboardPage.js   # Role-based dashboards
?   ?   ??? ProfilePage.js     # User profile
?   ??? services/
?   ?   ??? api.js             # API client & endpoints
?   ??? App.js                 # Main app component
?   ??? index.js               # Entry point
?   ??? index.css              # Global styles
??? .env                       # Environment variables
??? package.json               # Dependencies
??? tailwind.config.js         # Tailwind configuration
??? README.md                  # Documentation
```

---

## ?? Configuration

### API URL
Update `.env` file:
```
REACT_APP_API_URL=https://localhost:7068
```

For production:
```
REACT_APP_API_URL=https://your-api.azurewebsites.net
```

---

## ?? Testing

### Test with Default Accounts

**Student Account:**
- Email: `student355154426@test.com`
- Password: `TestPassword123!`
- Can access: Student Dashboard

**Recruiter Account:**
- Email: `recruiter1517664712@test.com`
- Password: `Password123!`
- Can access: Recruiter Dashboard

**Admin Account:**
- Email: `admin2123524763@test.com`
- Password: `Password123!`
- Can access: Admin Dashboard

### Create New Account
1. Click "Get Started" or "Sign up"
2. Fill in registration form
3. Choose role: Student, Recruiter, or Admin
4. Submit and you'll be auto-logged in

---

## ?? Security Features

- ? **JWT Token Storage** - Stored in localStorage
- ? **Auto Token Refresh** - Axios interceptors
- ? **Protected Routes** - Unauthorized redirect
- ? **Role-Based Access** - Component-level checks
- ? **Secure API Calls** - HTTPS only
- ? **Auto Logout** - On 401 errors

---

## ?? Customization

### Change Theme Colors
Edit `tailwind.config.js`:
```javascript
module.exports = {
  theme: {
    extend: {
      colors: {
        primary: '#your-color',
        secondary: '#your-color'
      }
    }
  }
}
```

### Add New Pages
1. Create component in `src/pages/`
2. Add route in `src/App.js`
3. Add navigation in `src/components/Navbar.js`

### Add New API Endpoints
Edit `src/services/api.js`:
```javascript
export const myAPI = {
  getData: () => api.get('/my-endpoint'),
  postData: (data) => api.post('/my-endpoint', data)
};
```

---

## ?? Deployment

### Build for Production
```bash
npm run build
```

### Deploy to Azure Static Web Apps
```bash
# Install Azure CLI
az login

# Create static web app
az staticwebapp create \
  --name studentlink-frontend \
  --resource-group rg-studentlink-proj \
  --source ./ \
  --location "East US" \
  --branch main
```

### Deploy to Netlify
1. Build: `npm run build`
2. Drag `build/` folder to Netlify
3. Set environment variable: `REACT_APP_API_URL`

### Deploy to Vercel
```bash
npm install -g vercel
vercel --prod
```

---

## ?? Available Scripts

| Command | Description |
|---------|-------------|
| `npm start` | Start development server |
| `npm test` | Run tests |
| `npm run build` | Build for production |
| `npm run eject` | Eject from Create React App |

---

## ?? Troubleshooting

### API Connection Error
**Problem:** "Failed to fetch" or CORS error

**Solution:**
1. Ensure API is running: `cd StudentLinkApi; dotnet run`
2. Check API URL in `.env` file
3. Verify CORS is enabled in API (already configured)

### Login Fails
**Problem:** "Invalid credentials"

**Solution:**
1. Use correct test account credentials
2. Or register a new account
3. Check API logs for errors

### White Screen
**Problem:** App doesn't load

**Solution:**
1. Check browser console for errors
2. Run `npm install` again
3. Delete `node_modules` and reinstall

### Port Already in Use
**Problem:** Port 3000 is busy

**Solution:**
```bash
# Windows
netstat -ano | findstr :3000
taskkill /PID <PID> /F

# Or use different port
set PORT=3001 && npm start
```

---

## ?? Next Steps

### Phase 2 Features to Add
1. **CV Upload** - File upload component
2. **Job Listings** - Browse and search jobs
3. **Applications** - Apply to jobs
4. **Messaging** - In-app chat
5. **Notifications** - Real-time updates

### Enhancements
- Add form validation library (Formik/React Hook Form)
- Add state management (Redux/Zustand)
- Add UI component library (Material-UI/Chakra UI)
- Add testing (Jest/React Testing Library)
- Add analytics (Google Analytics)

---

## ?? Resources

- **React Docs**: https://react.dev
- **React Router**: https://reactrouter.com
- **Tailwind CSS**: https://tailwindcss.com
- **Axios**: https://axios-http.com

---

## ?? Success!

You now have a fully functional frontend connected to your StudentLink API!

**Your complete stack:**
- ? .NET 9 API with JWT Auth
- ? SQL Database (LocalDB/Azure SQL)
- ? React Frontend with Modern UI
- ? Role-Based Access Control
- ? Responsive Design
- ? Production Ready

**Happy coding!** ??