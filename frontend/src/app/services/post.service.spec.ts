import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PostService } from './post.service';
import { AuthService } from './auth.service';
import { Post, LikeResponse } from '../models/comment.model';
import { environment } from '../../environments/environment';

describe('PostService', () => {
  let service: PostService;
  let authService: AuthService;
  let httpTesting: HttpTestingController;

  const baseUrl = environment.apiBaseUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService,
        PostService
      ]
    });

    service = TestBed.inject(PostService);
    authService = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should fetch posts list with auth headers when logged in', () => {
    authService.login({ id: 'user-1', name: 'User 1' });

    const mockPosts: Post[] = [
      {
        id: 'post-1',
        authorId: 'author-1',
        title: 'Title 1',
        content: 'Content 1',
        createdAt: '2026-11-06T10:00:00Z',
        likeCount: 5,
        isLiked: true
      }
    ];

    service.getPosts().subscribe(posts => {
      expect(posts.length).toBe(1);
      expect(posts[0].id).toBe('post-1');
      expect(posts[0].likeCount).toBe(5);
      expect(posts[0].isLiked).toBe(true);
    });

    const req = httpTesting.expectOne(`${baseUrl}/posts`);
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('X-User-Id')).toBe('user-1');
    req.flush(mockPosts);
  });

  it('should fetch a single post by id', () => {
    const mockPost: Post = {
      id: 'post-1',
      authorId: 'author-1',
      title: 'Title 1',
      content: 'Content 1',
      createdAt: '2026-11-06T10:00:00Z',
      likeCount: 2,
      isLiked: false
    };

    service.getPost('post-1').subscribe(post => {
      expect(post.id).toBe('post-1');
      expect(post.likeCount).toBe(2);
      expect(post.isLiked).toBe(false);
    });

    const req = httpTesting.expectOne(`${baseUrl}/posts/post-1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockPost);
  });

  it('should send POST request to toggle like on a post', () => {
    authService.login({ id: 'user-2', name: 'User 2' });

    const mockLikeResponse: LikeResponse = {
      postId: 'post-1',
      isLiked: true,
      likeCount: 3
    };

    service.toggleLike('post-1').subscribe(res => {
      expect(res.postId).toBe('post-1');
      expect(res.isLiked).toBe(true);
      expect(res.likeCount).toBe(3);
    });

    const req = httpTesting.expectOne(`${baseUrl}/posts/post-1/like`);
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('X-User-Id')).toBe('user-2');
    req.flush(mockLikeResponse);
  });
});
