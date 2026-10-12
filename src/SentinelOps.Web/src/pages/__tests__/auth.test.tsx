import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "@/context/auth-context";
import { LoginPage } from "@/pages/login";
import { RegisterPage } from "@/pages/register";

function renderWithAuth(ui: React.ReactElement) {
  return render(
    <BrowserRouter>
      <AuthProvider>{ui}</AuthProvider>
    </BrowserRouter>
  );
}

describe("Authentication Pages", () => {
  describe("LoginPage", () => {
    it("renders login form with email/username and password fields", () => {
      renderWithAuth(<LoginPage />);

      expect(screen.getByRole("heading", { name: "Sign In" })).toBeInTheDocument();
      expect(screen.getByLabelText(/Email or Username/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Password/i)).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Sign In" })).toBeInTheDocument();
      expect(screen.getByRole("link", { name: "Create one" })).toHaveAttribute("href", "/register");
    });

    it("allows typing into email and password inputs", () => {
      renderWithAuth(<LoginPage />);

      const emailInput = screen.getByLabelText(/Email or Username/i) as HTMLInputElement;
      const passInput = screen.getByLabelText(/Password/i) as HTMLInputElement;

      fireEvent.change(emailInput, { target: { value: "admin@sentinelops.local" } });
      fireEvent.change(passInput, { target: { value: "Secret123!" } });

      expect(emailInput.value).toBe("admin@sentinelops.local");
      expect(passInput.value).toBe("Secret123!");
    });
  });

  describe("RegisterPage", () => {
    it("renders registration form with all required inputs and role dropdown", () => {
      renderWithAuth(<RegisterPage />);

      expect(screen.getByRole("heading", { name: "Register Account" })).toBeInTheDocument();
      expect(screen.getByLabelText(/Full Name/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Username/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Work Email/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/^Password$/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Confirm Password/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Initial Role/i)).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Create Account" })).toBeInTheDocument();
    });

    it("shows password requirements indicator", () => {
      renderWithAuth(<RegisterPage />);

      expect(screen.getByText(/Min 8 characters/i)).toBeInTheDocument();
      expect(screen.getByText(/Uppercase letter/i)).toBeInTheDocument();
      expect(screen.getByText(/Number \(0-9\)/i)).toBeInTheDocument();
      expect(screen.getByText(/Special character/i)).toBeInTheDocument();
    });
  });
});
