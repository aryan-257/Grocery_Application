import { Component, inject, OnInit, OnDestroy, signal, computed, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '../../core/services/auth.service';
import { OrderService } from '../../core/services/order.service';
import { SupportTicket, SupportMessage } from '../../core/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-support',
  imports: [RouterLink, FormsModule, DatePipe],
  template: `
<div class="sp-page">

  <!-- Header -->
  <div class="sp-header">
    <div>
      <h1 class="sp-title">Support Center</h1>
      <p class="sp-sub">Get help with your orders and account</p>
    </div>
    @if (!selectedTicket()) {
      <button (click)="showNewForm.set(!showNewForm())" class="sp-btn-new">
        {{ showNewForm() ? 'Cancel' : '+ New Ticket' }}
      </button>
    } @else {
      <button (click)="selectedTicket.set(null)" class="sp-btn-back">&#x2190; All Tickets</button>
    }
  </div>

  <!-- Stats -->
  @if (!selectedTicket()) {
    <div class="sp-stats">
      <div class="sp-stat c1"><div class="ss-val">{{ tickets().length }}</div><div class="ss-lbl">Total Tickets</div></div>
      <div class="sp-stat c2"><div class="ss-val">{{ countByStatus('Open') }}</div><div class="ss-lbl">Open</div></div>
      <div class="sp-stat c3"><div class="ss-val">{{ countByStatus('InProgress') }}</div><div class="ss-lbl">In Progress</div></div>
      <div class="sp-stat c4"><div class="ss-val">{{ countByStatus('Resolved') }}</div><div class="ss-lbl">Resolved</div></div>
    </div>
  }

  <!-- New Ticket Form -->
  @if (showNewForm() && !selectedTicket()) {
    <div class="sp-card sp-form-card">
      <div class="sp-card-header">
        <span class="sp-card-title">Create New Ticket</span>
      </div>
      <div class="sp-form-grid">
        <div class="sp-fg sp-full"><label class="sp-label">Subject <span class="sp-req">*</span></label>
          <input class="sp-input" [(ngModel)]="newSubject" placeholder="Brief description of your issue" /></div>
        <div class="sp-fg"><label class="sp-label">Category</label>
          <select class="sp-input" [(ngModel)]="newCategory">
            <option value="Order">Order Issue</option>
            <option value="Payment">Payment</option>
            <option value="Delivery">Delivery</option>
            <option value="Product">Product</option>
            <option value="Other">Other</option>
          </select></div>
        <div class="sp-fg"><label class="sp-label">Related Order (optional)</label>
          <select class="sp-input" [(ngModel)]="newOrderId">
            <option value="">-- Select an order --</option>
            @for (o of orders(); track o.id) {
              <option [value]="o.id">#{{ o.id.slice(0,8).toUpperCase() }} &bull; {{ o.items[0]?.productName }}{{ o.items.length > 1 ? ' +' + (o.items.length - 1) + ' more' : '' }} &bull; &#x20B9;{{ o.totalAmount.toFixed(2) }}</option>
            }
          </select></div>
        <div class="sp-fg sp-full"><label class="sp-label">Description <span class="sp-req">*</span></label>
          <textarea class="sp-input sp-textarea" [(ngModel)]="newDescription" rows="4" placeholder="Describe your issue in detail..."></textarea></div>
      </div>
      <div class="sp-form-actions">
        <button (click)="createTicket()" [disabled]="submitting()" class="sp-btn-submit">
          {{ submitting() ? 'Submitting...' : 'Submit Ticket' }}
        </button>
        <button (click)="showNewForm.set(false)" class="sp-btn-cancel">Cancel</button>
      </div>
    </div>
  }

  <!-- Ticket Detail -->
  @if (selectedTicket()) {
    <div class="sp-detail">
      <div class="sp-chat-card">
        <div class="sp-chat-head">
          <div>
            <p class="sp-ticket-id">#{{ selectedTicket()!.id.slice(0,8).toUpperCase() }}</p>
            <h2 class="sp-chat-title">{{ selectedTicket()!.subject }}</h2>
            <div class="sp-chips">
              <span class="sp-chip" [class]="'sc-' + selectedTicket()!.status.toLowerCase()">{{ selectedTicket()!.status }}</span>
              <span class="sp-chip" [class]="'pc-' + selectedTicket()!.priority.toLowerCase()">{{ selectedTicket()!.priority }}</span>
              <span class="sp-chip sp-cat-chip">{{ selectedTicket()!.category }}</span>
            </div>
          </div>
          <span class="sp-date">{{ selectedTicket()!.createdAt | date:'dd MMM yyyy' }}</span>
        </div>
        <div #msgContainer class="sp-messages">
          @for (msg of messages(); track msg.id) {
            <div class="sp-msg-row" [class.sp-msg-staff]="msg.isStaff" [class.sp-msg-user]="!msg.isStaff">
              <div class="sp-bubble" [class.sp-bubble-staff]="msg.isStaff" [class.sp-bubble-user]="!msg.isStaff">
                <p class="sp-msg-sender">{{ msg.isStaff ? msg.senderName + ' (Support)' : 'You' }}</p>
                <p class="sp-msg-text">{{ msg.message }}</p>
                <p class="sp-msg-time">{{ msg.createdAt | date:'dd MMM, HH:mm' }}</p>
              </div>
            </div>
          }
          @if (messages().length === 0) {
            <div class="sp-no-msgs">No messages yet. Describe your issue below.</div>
          }
        </div>
        @if (selectedTicket()!.status !== 'Closed' && selectedTicket()!.status !== 'Resolved') {
          <div class="sp-reply-bar">
            <input class="sp-reply-input" [(ngModel)]="replyText" (keyup.enter)="sendReply()" placeholder="Type your message..." />
            <button (click)="sendReply()" [disabled]="!replyText.trim() || sending()" class="sp-btn-send">
              {{ sending() ? '...' : 'Send' }}
            </button>
          </div>
        } @else {
          <div class="sp-closed-bar">This ticket is {{ selectedTicket()!.status.toLowerCase() }}</div>
        }
      </div>
    </div>
  }

  <!-- Tickets List -->
  @if (!selectedTicket()) {
    @if (loading()) {
      <div class="sp-skels">@for (i of [1,2,3]; track i) { <div class="sp-skel"></div> }</div>
    } @else if (tickets().length === 0) {
      <div class="sp-empty">
        <p class="sp-empty-icon">&#x1F4AC;</p>
        <p class="sp-empty-title">No support tickets yet</p>
        <p class="sp-empty-sub">Create a ticket and our team will help you</p>
        <button (click)="showNewForm.set(true)" class="sp-btn-submit">Create First Ticket</button>
      </div>
    } @else {
      <div class="sp-list">
        @for (ticket of tickets(); track ticket.id) {
          <div class="sp-ticket-row" (click)="openTicket(ticket)">
            <div class="sp-ticket-left">
              <p class="sp-ticket-id">#{{ ticket.id.slice(0,8).toUpperCase() }}</p>
              <p class="sp-ticket-subject">{{ ticket.subject }}</p>
              <div class="sp-chips" style="margin-top:6px">
                <span class="sp-chip" [class]="'sc-' + ticket.status.toLowerCase()">{{ ticket.status }}</span>
                <span class="sp-chip" [class]="'pc-' + ticket.priority.toLowerCase()">{{ ticket.priority }}</span>
                <span class="sp-chip sp-cat-chip">{{ ticket.category }}</span>
              </div>
            </div>
            <div class="sp-ticket-right">
              <span class="sp-ticket-date">{{ ticket.createdAt | date:'dd MMM' }}</span>
              <span class="sp-msg-count">{{ ticket.messageCount }} msg{{ ticket.messageCount !== 1 ? 's' : '' }}</span>
              <span class="sp-arrow">&#x2192;</span>
            </div>
          </div>
        }
      </div>
    }
  }
</div>
  `
  ,styles: [`
    * { box-sizing:border-box; }
    .sp-page { padding:28px; min-height:100vh; background:var(--adm-bg); color:var(--adm-text); max-width:860px; margin:0 auto; }
    .sp-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:24px; }
    .sp-title { font-size:26px; font-weight:800; margin:0; background:linear-gradient(135deg,#38bdf8,#818cf8); -webkit-background-clip:text; -webkit-text-fill-color:transparent; }
    .sp-sub { margin:4px 0 0; font-size:13px; color:var(--adm-text3); }
    .sp-btn-new { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:10px 20px; border-radius:10px; font-size:14px; font-weight:700; cursor:pointer; box-shadow:0 4px 12px rgba(34,197,94,.25); transition:all .2s; }
    .sp-btn-new:hover { transform:translateY(-1px); }
    .sp-btn-back { background:var(--adm-card); color:var(--adm-text2); border:1px solid var(--adm-border2); padding:9px 18px; border-radius:9px; font-size:13px; font-weight:600; cursor:pointer; transition:all .2s; }
    .sp-btn-back:hover { background:var(--adm-border); }

    .sp-stats { display:grid; grid-template-columns:repeat(4,1fr); gap:12px; margin-bottom:24px; }
    .sp-stat { border-radius:12px; padding:16px; border:1px solid var(--adm-border); text-align:center; }
    .c1{background:var(--adm-s1);} .c2{background:var(--adm-s1);} .c3{background:var(--adm-s3);} .c4{background:var(--adm-s2);}
    .ss-val { font-size:26px; font-weight:800; color:var(--adm-stat-val); }
    .ss-lbl { font-size:12px; color:var(--adm-stat-lbl); margin-top:4px; font-weight:600; }

    .sp-card { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:16px; overflow:hidden; margin-bottom:20px; }
    .sp-card-header { padding:16px 20px; border-bottom:1px solid var(--adm-border); }
    .sp-card-title { font-size:15px; font-weight:700; color:var(--adm-text); }
    .sp-form-card { padding:0; }
    .sp-form-grid { display:grid; grid-template-columns:1fr 1fr; gap:14px; padding:20px; }
    .sp-fg { display:flex; flex-direction:column; gap:5px; }
    .sp-full { grid-column:1/-1; }
    .sp-label { font-size:11px; font-weight:700; color:var(--adm-text2); text-transform:uppercase; letter-spacing:.05em; }
    .sp-req { color:#ef4444; }
    .sp-input { background:var(--adm-input-bg); border:2px solid var(--adm-border2); color:var(--adm-text); padding:10px 13px; border-radius:8px; font-size:14px; width:100%; transition:border-color .2s; }
    .sp-input:focus { outline:none; border-color:#22c55e; box-shadow:0 0 0 3px rgba(34,197,94,.1); }
    .sp-textarea { resize:vertical; min-height:100px; font-family:inherit; }
    .sp-form-actions { display:flex; gap:12px; padding:0 20px 20px; }
    .sp-btn-submit { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:10px 24px; border-radius:9px; font-size:14px; font-weight:700; cursor:pointer; box-shadow:0 4px 12px rgba(34,197,94,.25); transition:all .2s; }
    .sp-btn-submit:hover:not(:disabled) { transform:translateY(-1px); }
    .sp-btn-submit:disabled { opacity:.5; cursor:not-allowed; }
    .sp-btn-cancel { background:var(--adm-card2); color:var(--adm-text2); border:1px solid var(--adm-border2); padding:10px 20px; border-radius:9px; font-size:14px; font-weight:600; cursor:pointer; }

    .sp-list { display:flex; flex-direction:column; gap:12px; }
    .sp-ticket-row { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:14px; padding:18px 20px; display:flex; justify-content:space-between; align-items:flex-start; cursor:pointer; transition:all .2s; }
    .sp-ticket-row:hover { border-color:#22c55e; box-shadow:0 4px 16px rgba(0,0,0,.1); transform:translateY(-1px); }
    .sp-ticket-left { flex:1; min-width:0; }
    .sp-ticket-id { font-family:monospace; font-size:11px; color:var(--adm-text3); margin:0 0 4px; }
    .sp-ticket-subject { font-size:15px; font-weight:700; color:var(--adm-text); margin:0; }
    .sp-ticket-right { display:flex; flex-direction:column; align-items:flex-end; gap:6px; flex-shrink:0; margin-left:16px; }
    .sp-ticket-date { font-size:12px; color:var(--adm-text2); }
    .sp-msg-count { font-size:11px; color:var(--adm-text3); background:var(--adm-border); padding:2px 8px; border-radius:20px; }
    .sp-arrow { font-size:16px; color:var(--adm-text3); }

    .sp-chips { display:flex; flex-wrap:wrap; gap:6px; }
    .sp-chip { padding:3px 10px; border-radius:20px; font-size:11px; font-weight:700; }
    .sc-open{background:rgba(59,130,246,.15);color:#1d4ed8;border:1px solid rgba(59,130,246,.3);}
    .sc-inprogress{background:rgba(251,191,36,.15);color:#b45309;border:1px solid rgba(251,191,36,.3);}
    .sc-resolved{background:rgba(34,197,94,.15);color:#15803d;border:1px solid rgba(34,197,94,.3);}
    .sc-closed{background:rgba(100,116,139,.15);color:#475569;border:1px solid rgba(100,116,139,.3);}
    .pc-high{background:rgba(220,38,38,.15);color:#dc2626;border:1px solid rgba(220,38,38,.3);}
    .pc-medium{background:rgba(251,191,36,.15);color:#b45309;border:1px solid rgba(251,191,36,.3);}
    .pc-low{background:rgba(100,116,139,.15);color:#475569;border:1px solid rgba(100,116,139,.3);}
    .sp-cat-chip{background:var(--adm-border);color:var(--adm-text2);}

    .sp-detail { display:flex; flex-direction:column; gap:16px; }
    .sp-chat-card { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:16px; overflow:hidden; }
    .sp-chat-head { padding:18px 20px; border-bottom:1px solid var(--adm-border); background:linear-gradient(135deg,var(--adm-card2),var(--adm-card)); display:flex; justify-content:space-between; align-items:flex-start; }
    .sp-chat-title { font-size:16px; font-weight:700; color:var(--adm-text); margin:4px 0 8px; }
    .sp-date { font-size:12px; color:var(--adm-text2); flex-shrink:0; }
    .sp-messages { min-height:300px; max-height:420px; overflow-y:auto; padding:16px 20px; display:flex; flex-direction:column; gap:12px; background:var(--adm-bg); }
    .sp-msg-row { display:flex; }
    .sp-msg-staff { justify-content:flex-start; }
    .sp-msg-user { justify-content:flex-end; }
    .sp-bubble { max-width:70%; padding:12px 16px; border-radius:14px; }
    .sp-bubble-staff { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:14px 14px 14px 4px; }
    .sp-bubble-user { background:linear-gradient(135deg,#22c55e,#16a34a); border-radius:14px 14px 4px 14px; }
    .sp-msg-sender { font-size:11px; font-weight:700; margin:0 0 4px; color:var(--adm-text2); }
    .sp-bubble-user .sp-msg-sender { color:rgba(255,255,255,.7); }
    .sp-msg-text { font-size:13.5px; margin:0 0 6px; color:var(--adm-text); line-height:1.5; }
    .sp-bubble-user .sp-msg-text { color:#fff; }
    .sp-msg-time { font-size:10px; margin:0; color:var(--adm-text3); }
    .sp-bubble-user .sp-msg-time { color:rgba(255,255,255,.5); }
    .sp-no-msgs { text-align:center; color:var(--adm-text2); font-size:14px; padding:40px; }
    .sp-reply-bar { display:flex; gap:10px; padding:14px 20px; border-top:1px solid var(--adm-border); background:var(--adm-card2); }
    .sp-reply-input { flex:1; background:var(--adm-input-bg); border:2px solid var(--adm-border2); color:var(--adm-text); padding:10px 14px; border-radius:8px; font-size:14px; }
    .sp-reply-input:focus { outline:none; border-color:#22c55e; }
    .sp-btn-send { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:10px 20px; border-radius:8px; font-weight:700; font-size:14px; cursor:pointer; }
    .sp-btn-send:disabled { opacity:.5; cursor:not-allowed; }
    .sp-closed-bar { text-align:center; padding:14px; color:var(--adm-text2); font-size:13px; border-top:1px solid var(--adm-border); background:var(--adm-card2); }

    .sp-empty { text-align:center; padding:60px 20px; display:flex; flex-direction:column; align-items:center; gap:10px; }
    .sp-empty-icon { font-size:52px; }
    .sp-empty-title { font-size:18px; font-weight:700; color:var(--adm-text); margin:0; }
    .sp-empty-sub { font-size:14px; color:var(--adm-text2); margin:0; }
    .sp-skels { display:flex; flex-direction:column; gap:12px; }
    .sp-skel { height:100px; background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }
  `]
})
export class Support implements OnInit, OnDestroy, AfterViewChecked {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private orderService = inject(OrderService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = `${environment.apiUrl}/api/v1`;
  private hubUrl = environment.hubUrl;
  private hub?: signalR.HubConnection;
  private shouldScroll = false;

  @ViewChild('msgContainer') msgContainer?: ElementRef<HTMLDivElement>;

  tickets = signal<SupportTicket[]>([]);
  selectedTicket = signal<SupportTicket | null>(null);
  messages = signal<SupportMessage[]>([]);
  loading = signal(true);
  submitting = signal(false);
  sending = signal(false);
  showNewForm = signal(false);

  countByStatus = (s: string) => this.tickets().filter(t => t.status === s).length;

  newSubject = '';
  newCategory = 'Order';
  newPriority = 'Medium';
  newOrderId = '';
  newDescription = '';
  replyText = '';

  orders = signal<any[]>([]);

  ngOnInit() {
    this.loadTickets();
    this.orderService.getOrders().subscribe(o => this.orders.set(o));
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadTicketById(id);
  }

  ngAfterViewChecked() {
    if (this.shouldScroll && this.msgContainer) {
      const el = this.msgContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  ngOnDestroy() {
    this.hub?.stop();
  }

  loadTickets() {
    this.loading.set(true);
    this.http.get<SupportTicket[]>(`${this.api}/support/tickets`).subscribe({
      next: t => { this.tickets.set(t); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  loadTicketById(id: string) {
    this.http.get<{ ticket: SupportTicket; messages: SupportMessage[] }>(`${this.api}/support/tickets/${id}`).subscribe({
      next: res => {
        this.selectedTicket.set(res.ticket);
        this.messages.set(res.messages);
        this.shouldScroll = true;
        this.connectHub(id);
      }
    });
  }

  openTicket(ticket: SupportTicket) {
    this.router.navigate(['/support', ticket.id]);
    this.loadTicketById(ticket.id);
  }

  connectHub(ticketId: string) {
    const token = this.auth.getAccessToken();
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/support`, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .build();

    this.hub.on('newMessage', (msg: SupportMessage) => {
      this.messages.update(m => [...m, msg]);
      this.shouldScroll = true;
    });

    this.hub.on('ticketUpdated', (update: Partial<SupportTicket>) => {
      this.selectedTicket.update(t => t ? { ...t, ...update } : t);
      this.tickets.update(list => list.map(t => t.id === update.id ? { ...t, ...update } : t));
    });

    this.hub.start().then(() => this.hub!.invoke('JoinTicket', ticketId)).catch(console.error);
  }

  createTicket() {
    if (!this.newSubject.trim() || !this.newDescription.trim()) return;
    this.submitting.set(true);
    const orderPrefix = this.newOrderId
      ? `[Order: #${this.newOrderId.slice(0,8).toUpperCase()}]\n\n`
      : '';
    this.http.post<SupportTicket>(`${this.api}/support/tickets`, {
      subject: this.newSubject,
      category: this.newCategory,
      description: orderPrefix + this.newDescription,
      priority: this.newPriority
    }).subscribe({
      next: ticket => {
        this.submitting.set(false);
        this.showNewForm.set(false);
        this.newSubject = ''; this.newDescription = ''; this.newOrderId = '';
        this.loadTickets();
        this.openTicket(ticket);
      },
      error: () => this.submitting.set(false)
    });
  }

  sendReply() {
    if (!this.replyText.trim() || !this.selectedTicket()) return;
    this.sending.set(true);
    const id = this.selectedTicket()!.id;
    this.http.post<SupportMessage>(`${this.api}/support/tickets/${id}/messages`, { message: this.replyText }).subscribe({
      next: msg => {
        this.messages.update(m => [...m, msg]);
        this.replyText = '';
        this.sending.set(false);
        this.shouldScroll = true;
      },
      error: () => this.sending.set(false)
    });
  }

  statusClass(status: string) {
    const map: Record<string, string> = {
      Open: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      InProgress: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
      Resolved: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
      Closed: 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'
    };
    return map[status] ?? 'bg-gray-100 text-gray-500';
  }

  priorityClass(priority: string) {
    const map: Record<string, string> = {
      High: 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400',
      Medium: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400',
      Low: 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'
    };
    return map[priority] ?? 'bg-gray-100 text-gray-500';
  }
}
