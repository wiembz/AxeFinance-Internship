import { HttpInterceptorFn } from '@angular/common/http';

export const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
  // Send cookies with requests to our API
  if (req.url.startsWith('http://localhost:5108/')) {
    const reqWithCredentials = req.clone({
      setHeaders: {},
      withCredentials: true
    });
    return next(reqWithCredentials);
  }

  return next(req);
};
