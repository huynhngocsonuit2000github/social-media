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

export interface AppNotification {
  id: string;
  recipientUserId: string;
  actorUserId: string;
  actorName: string;
  postId: string;
  commentId: string;
  message: string;
  createdAt: string;
  isRead: boolean;
}

export interface User {
  id: string;
  name: string;
}

export interface ErrorResponse {
  error: string;
}
