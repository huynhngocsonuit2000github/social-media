import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Login } from './login';
import { AuthService } from '../../services/auth.service';

describe('Login Component', () => {
  let fixture: ComponentFixture<Login>;
  let component: Login;
  let authService: AuthService;
  let router: Router;

  beforeEach(async () => {
    const routerMock = {
      navigate: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        AuthService,
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create the login component', () => {
    expect(component).toBeTruthy();
  });

  it('should log in and navigate to root on onLogin()', () => {
    expect(authService.isLoggedIn()).toBe(false);

    component.onLogin();

    expect(authService.isLoggedIn()).toBe(true);
    expect(authService.currentUser()?.name).toBe('Nguyễn Văn A');
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should navigate to root on goHome()', () => {
    component.goHome();
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });
});
