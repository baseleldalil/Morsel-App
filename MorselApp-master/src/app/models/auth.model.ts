export interface LoginRequest {
  email: string;
  password: string;
}

export interface User {
  email: string;
  name: string;
  subscriptionPlan: string;
  lastLoginAt: string;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  apiKey: string;
  user: User;
}
