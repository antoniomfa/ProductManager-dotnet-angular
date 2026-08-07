/**
 * Where the API lives.
 *
 * Relative on purpose. In development proxy.conf.json forwards /api to the .NET API on port
 * 5186; in production the app is served from the same origin. Point this at an absolute URL if
 * the two are ever deployed separately.
 */
export const API_BASE_URL = '/api';
