/** @type {import('next').NextConfig} */
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
};
module.exports = nextConfig;
