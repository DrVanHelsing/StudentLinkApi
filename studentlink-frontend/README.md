# StudentLink Frontend

Modern React frontend for the StudentLink API with authentication and role-based dashboards.

## Features

- ?? User Authentication (Login/Register)
- ?? Role-based Dashboards (Student/Recruiter/Admin)
- ?? Modern UI with Tailwind CSS
- ?? Responsive Design
- ?? Protected Routes
- ?? JWT Token Management

## Quick Start

```bash
# Install dependencies
npm install

# Start development server
npm start

# Build for production
npm run build
```

## Technology Stack

- **React 18** - UI Framework
- **React Router** - Navigation
- **Axios** - HTTP Client
- **Tailwind CSS** - Styling
- **Context API** - State Management

## Project Structure

```
studentlink-frontend/
??? public/
??? src/
?   ??? components/     # Reusable components
?   ??? pages/         # Page components
?   ??? contexts/      # React contexts (Auth)
?   ??? services/      # API services
?   ??? utils/         # Utility functions
?   ??? App.js         # Main app component
??? package.json
```

## API Configuration

Update `src/services/api.js` with your API URL:

```javascript
const API_URL = 'https://localhost:7068';
```

## Available Scripts

- `npm start` - Run development server
- `npm test` - Run tests
- `npm run build` - Build for production
- `npm run eject` - Eject from Create React App

## Environment Variables

Create a `.env` file:

```
REACT_APP_API_URL=https://localhost:7068
```

## Default Users (for testing)

- **Student**: student355154426@test.com / TestPassword123!
- **Recruiter**: recruiter1517664712@test.com / Password123!
- **Admin**: admin2123524763@test.com / Password123!