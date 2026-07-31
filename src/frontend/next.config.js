/** @type {import('next').NextConfig} */
const API_TARGET = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

const nextConfig = {
  reactStrictMode: true,
  output: 'standalone',
  // DEC-110: Image optimization config
  // No <img> tags currently in app (uses lucide-react icons), but config ready for future
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'Anas-Assasket-erp-system.hf.space',
      },
    ],
    formats: ['image/avif', 'image/webp'],
  },
  // Phase 6.2 fix: proxy /api/* from Next.js dev server (port 3000) to backend (port 5000).
  // Many admin pages use relative fetch('/api/...') and would otherwise 404 against Next.js.
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: `${API_TARGET}/api/:path*`,
      },
    ];
  },
};
module.exports = nextConfig;
