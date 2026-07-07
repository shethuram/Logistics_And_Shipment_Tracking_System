export interface NotificationDto {
  id: string;
  shipmentId: string | null;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface MyNotificationsResponse {
  data: NotificationDto[];
  unreadCount: number;
  total: number;
  page: number;
  pageSize: number;
}

export interface MarkReadResponse {
  id: string;
  isRead: boolean;
}
