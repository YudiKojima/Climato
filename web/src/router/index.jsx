import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Home from "../pages/Home.jsx";
import Favorites from "../pages/Favorites.jsx";
import WeatherDetails from "../pages/WeatherDetails.jsx";
import SignIn from "../pages/SignIn.jsx";
import SignOut from "../pages/SignOut.jsx";
import ProtectedRoute from "./ProtectedRoute.jsx";
import SignUp from "../pages/SignUp.jsx";

export default function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/signin" element={<SignIn />} />
        <Route path="/signup" element={<SignUp />} />
        <Route path="/signout" element={<SignOut />} />

        <Route
          path="/p"
          element={
            <ProtectedRoute>
              <Home />
            </ProtectedRoute>
          }
        />
        <Route
          path="/p/weather/:cityName"
          element={
            <ProtectedRoute>
              <WeatherDetails />
            </ProtectedRoute>
          }
        />
        <Route
          path="/p/favorites"
          element={
            <ProtectedRoute>
              <Favorites />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<Navigate to="/signin" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
