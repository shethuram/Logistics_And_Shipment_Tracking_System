import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAuth0, authHttpInterceptorFn } from '@auth0/auth0-angular';
import { environment } from '../environments/environment';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authHttpInterceptorFn])),
    provideAuth0({
      domain: environment.auth0.domain,
      clientId: environment.auth0.clientId,
      cacheLocation: 'localstorage',
      useRefreshTokens: true,
      authorizationParams: {
        redirect_uri: window.location.origin + '/callback',
        audience: environment.auth0.audience,
        scope: 'openid profile email offline_access'
      },
      httpInterceptor: {
        allowedList: [
          {
            uri: `${environment.apiBaseUrl}/api/auth*`,
            tokenOptions: {
              authorizationParams: {
                audience: environment.auth0.audience,
                scope: 'openid profile email offline_access'
              }
            }
          },
          {
            uri: `${environment.apiBaseUrl}/api/shipments*`,
            tokenOptions: {
              authorizationParams: {
                audience: environment.auth0.audience,
                scope: 'openid profile email offline_access'
              }
            }
          },
          {
            uri: `${environment.apiBaseUrl}/api/drivers*`,
            tokenOptions: {
              authorizationParams: {
                audience: environment.auth0.audience,
                scope: 'openid profile email offline_access'
              }
            }
          },
          {
            uri: `${environment.apiBaseUrl}/api/payments*`,
            tokenOptions: {
              authorizationParams: {
                audience: environment.auth0.audience,
                scope: 'openid profile email offline_access'
              }
            }
          },
          {
            uri: `${environment.apiBaseUrl}/api/notifications*`,
            tokenOptions: {
              authorizationParams: {
                audience: environment.auth0.audience,
                scope: 'openid profile email offline_access'
              }
            }
          },
          {
            uri: `${environment.apiBaseUrl}/api/disputes*`,
            tokenOptions: {
              authorizationParams: {
                audience: environment.auth0.audience,
                scope: 'openid profile email offline_access'
              }
            }
          },
          {
            uri: `${environment.apiBaseUrl}/api/admin*`,
            tokenOptions: {
              authorizationParams: {
                audience: environment.auth0.audience,
                scope: 'openid profile email offline_access'
              }
            }
          },
          {
            uri: `${environment.apiBaseUrl}/api/tracking*`,
            tokenOptions: {
              authorizationParams: {
                audience: environment.auth0.audience,
                scope: 'openid profile email offline_access'
              }
            }
          }
        ]
      }
    })
  ]
};
