import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "MPFAI | Partner delivery workspace",
  description: "Explainable funding, delivery, attribution, evidence, and claims workspace.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
