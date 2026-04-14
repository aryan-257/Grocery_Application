import { Injectable, inject, signal, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import * as signalR from '@microsoft/signalr';
import { AppNotification } from '../models';
import { environment } from '../../../environments/environment';

/**
 * Service responsible for real-time in-app notifications.
 * Manages a SignalR WebSocket connection to the NotificationService hub,
 * maintains a reactive list of notifications and an unread count signal,
 * and provides methods for marking notifications as read, deleting them,
 * and navigating to their associated deep-link.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  /**
   * Reactive signal holding the current list of notifications (up to 50, newest first).
   * Updated when new notifications arrive via SignalR or when the list is loaded from the API.
   */
  notifications = signal<AppNotification[]>([]);

  /**
   * Reactive signal holding the count of unread notifications.
   * Used to display the badge number on the notification bell icon in the navbar.
   */
  unreadCount = signal(0);

  /**
   * Reactive signal indicating whether the SignalR hub connection is currently active.
   * Used by the UI to show a connection status indicator if needed.
   */
  connected = signal(false);

  /** The active SignalR hub connection. Null when disconnected. */
  private hub: signalR.HubConnection | null = null;

  /**
   * Initialises the notification service after login.
   * Clears any previous user's state first, then loads notifications and connects SignalR.
   * No-op in SSR environments.
   * @param token - The user's JWT access token, used to authenticate the SignalR connection.
   */
  init(token: string) {
    if (!isPlatformBrowser(this.platformId)) return;
    // Always clear previous user state before loading new user's notifications
    this.notifications.set([]);
    this.unreadCount.set(0);
    this.loadAll();
    this.connect(token);
  }

  /**
   * Establishes a SignalR WebSocket connection to the notification hub.
   * Configures automatic reconnect with exponential backoff (0, 2s, 5s, 10s).
   * Listens for incoming `notification` events and prepends them to the signal.
   * Stops any existing connection before creating a new one.
   */
  private connect(token: string) {
    if (this.hub) {
      this.hub.stop();
      this.hub = null;
    }

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`, {
        accessTokenFactory: () => token,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hub.on('notification', (n: AppNotification) => {
      this.notifications.update(list => [n, ...list].slice(0, 50));
      if (!n.isRead) this.unreadCount.update(c => c + 1);
    });

    this.hub.onreconnected(() => this.connected.set(true));
    this.hub.onclose(() => this.connected.set(false));

    this.hub.start()
      .then(() => this.connected.set(true))
      .catch(() => this.connected.set(false));
  }

  /**
   * Fetches all notifications for the authenticated user from the REST API.
   * Replaces the full notification list and recalculates unread count from scratch.
   * Called on init and can be called to manually refresh the notification list.
   */
  loadAll() {
    this.http.get<AppNotification[]>(`${environment.apiUrl}/api/v1/notifications`).subscribe({
      next: list => {
        this.notifications.set(list);
        // Always recalculate from the fresh list — never accumulate stale counts
        this.unreadCount.set(list.filter(n => !n.isRead).length);
      },
      error: () => {
        this.notifications.set([]);
        this.unreadCount.set(0);
      }
    });
  }

  /**
   * Marks a single notification as read both locally and on the backend.
   * Decrements the unread count. No-op if the notification is already read.
   * @param id - The notification's unique identifier.
   */
  markRead(id: string) {
    const n = this.notifications().find(n => n.id === id);
    if (n && !n.isRead) {
      this.http.patch(`${environment.apiUrl}/api/v1/notifications/${id}/read`, {}).subscribe();
      this.notifications.update(list => list.map(x => x.id === id ? { ...x, isRead: true } : x));
      this.unreadCount.update(c => Math.max(0, c - 1));
    }
  }

  /**
   * Marks all notifications as read both locally and on the backend.
   * Resets the unread count to 0.
   */
  markAllRead() {
    this.http.patch(`${environment.apiUrl}/api/v1/notifications/read-all`, {}).subscribe();
    this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
    this.unreadCount.set(0);
  }

  /**
   * Permanently deletes a notification both locally and on the backend.
   * Decrements the unread count if the deleted notification was unread.
   * @param id - The notification's unique identifier.
   */
  delete(id: string) {
    const n = this.notifications().find(n => n.id === id);
    this.http.delete(`${environment.apiUrl}/api/v1/notifications/${id}`).subscribe();
    this.notifications.update(list => list.filter(x => x.id !== id));
    if (n && !n.isRead) this.unreadCount.update(c => Math.max(0, c - 1));
  }

  /**
   * Deletes all notifications for the current user both locally and on the backend.
   * Resets the notifications list and unread count to empty/zero.
   */
  clearAll() {
    this.http.delete(`${environment.apiUrl}/api/v1/notifications`).subscribe();
    this.notifications.set([]);
    this.unreadCount.set(0);
  }

  /**
   * Marks a notification as read and navigates to its associated deep-link URL.
   * Used when the user clicks a notification in the dropdown.
   * @param n - The notification to navigate from.
   */
  navigate(n: AppNotification) {
    this.markRead(n.id);
    if (n.link) this.router.navigateByUrl(n.link);
  }

  /**
   * Disconnects the SignalR hub and resets all notification state.
   * Called on logout to clean up the connection and clear sensitive data.
   */
  disconnect() {
    this.hub?.stop();
    this.hub = null;
    this.notifications.set([]);
    this.unreadCount.set(0);
    this.connected.set(false);
  }
}
