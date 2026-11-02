export interface Comment {
  id: string;
  postId: string;
  userId: string;
  authorName: string;
  content: string;
  createdAt: string;
}

export interface Post {
  id: string;
  authorId: string;
  title: string;
  content: string;
  createdAt: string;
}

export interface User {
  id: string;
  name: string;
}

export interface ErrorResponse {
  error: string;
}
