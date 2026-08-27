// Sprint 63 / 65 — Jest configuration (FE tests for Sprint 63 Wave 3A and beyond).
//
// Run with: `npm test` (or `npm test -- SmartSidebar` to filter).
// The tests live in `src/frontend/tests/**` and `*.test.{ts,tsx}`.
//
// Stack:
//   - jest 29 + ts-jest-free via babel-jest (uses the Next.js babel preset
//     that already ships with the project)
//   - jest-environment-jsdom for component tests
//   - @testing-library/react + @testing-library/jest-dom for DOM assertions
//   - identity-obj-proxy for CSS-module mocks (none used yet, kept for future)

/** @type {import('jest').Config} */
module.exports = {
  testEnvironment: 'jsdom',
  rootDir: __dirname,
  roots: ['<rootDir>/tests', '<rootDir>/hooks', '<rootDir>/components', '<rootDir>/lib'],
  // Match `*.test.ts`, `*.test.tsx`, and `*.spec.tsx` — but only inside
  // the FE tree (we don't want to run node_modules tests).
  // Use testRegex because the testMatch glob with <rootDir>/... prefixes
  // is resolved as absolute paths on Windows, which Jest does not match.
  testRegex: '(/__tests__/.*|\\.(test|spec))\\.(ts|tsx)$',
  testPathIgnorePatterns: [
    '/node_modules/',
    '/.next/',
    '/dist/',
  ],
  moduleNameMapper: {
    // Match tsconfig.json path alias `@/*` → `./*`
    '^@/(.*)$': '<rootDir>/$1',
    // CSS / asset stubs
    '\\.(css|less|scss|sass)$': 'identity-obj-proxy',
    '\\.(jpg|jpeg|png|gif|svg)$': '<rootDir>/tests/__mocks__/fileMock.js',
  },
  transform: {
    // Use babel-jest with the Next.js babel preset. This is the same
    // transform Next.js uses for the dev server, so test transpilation
    // matches the dev runtime as closely as possible.
    '^.+\\.(ts|tsx|js|jsx)$': ['babel-jest', { presets: ['next/babel'] }],
  },
  transformIgnorePatterns: [
    // node_modules is transformed only when needed for ESM-only packages
    // (e.g. lucide-react). Default is fine for the current dependency set.
    '/node_modules/',
  ],
  testTimeout: 10_000,
  // Load @testing-library/jest-dom matchers (toBeInTheDocument, toHaveClass, ...).
  setupFilesAfterEnv: ['<rootDir>/tests/setup.ts'],
  // Reporters — keep it simple.
  verbose: true,
};
