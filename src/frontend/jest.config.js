﻿// Sprint 63 / 65 — Jest configuration (FE tests for Sprint 63 Wave 3A and beyond).
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
  roots: ['<rootDir>/tests'],
  testRegex: '(/__tests__/.*|\\.(test|spec))\\.(ts|tsx)$',
  testPathIgnorePatterns: [
    '/node_modules/',
    '/.next/',
    '/dist/',
  ],
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/$1',
    '\\.(css|less|scss|sass)$': 'identity-obj-proxy',
    '\\.(jpg|jpeg|png|gif|svg)$': '<rootDir>/tests/__mocks__/fileMock.js',
  },
  transform: {
    '^.+\\.(ts|tsx|js|jsx)$': ['babel-jest', { presets: ['next/babel'] }],
  },
  transformIgnorePatterns: [
    '/node_modules/',
  ],
  testTimeout: 10_000,
  setupFilesAfterEnv: ['<rootDir>/tests/setup.ts'],
  verbose: true,
};
