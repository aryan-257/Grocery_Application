import { Component, inject, OnInit, OnDestroy, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '../../../core/services/auth.service';
import { SupportTicket, SupportMessage } from '../../../core/models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-support',
  standalone: true,
  imports: [FormsModule, CommonModule, DatePipe],
  template: `
<div class="page">
  <!-- Header -->
  <div class="page-header">
    <div>
      <h1 class="page-title">Support Tickets</h1>
      <p class="page-sub">Manage customer support requests</p>
    </div>
    @if (selectedTicket()) {
      <button class="btn-back" (click)="selectedTicket.set(null); messages.set([])">← All Tickets</button>
    }
  </div>

  @if (!selectedTicket()) {
    <!-- Stats -->
    <div class="stats">
      <div class="stat-card c1"><div class="stat-icon">🎫</div><div><div class="stat-val">{{ tickets().length }}</div><div class="stat-lbl">Total</div></div></div>
      <div class="stat-card c2"><div class="stat-icon">🔵</div><div><div class="stat-val">{{ countByStatus('Open') }}</div><div class="stat-lbl">Open</div></div></div>
      <div class="stat-card c3"><div class="stat-icon">🟡</div><div><div class="stat-val">{{ countByStatus('InProgress') }}</div><div class="stat-lbl">In Progress</div></div></div>
      <div class="stat-card c4"><div class="stat-icon">✅</div><div><div class="stat-val">{{ countByStatus('Resolved') }}</div><div class="stat-lbl">Resolved</div></div></div>
    </div>

    <!-- Filters -->
    <div class="filters">
      <select class="fsel" [(ngModel)]="filterStatus" (ngModelChange)="loadTickets()">
        <option value="">All Status</option>
        <option value="Open">Open</option>
        <option value="InProgress">In Progress</option>
        <option value="Resolved">Resolved</option>
        <option value="Closed">Closed</option>
      </select>
      <select class="fsel" [(ngModel)]="filterPriority" (ngModelChange)="loadTickets()">
        <option value="">All Priority</option>
        <option value="High">High</option>
        <option value="Medium">Medium</option>
        <option value="Low">Low</option>
      </select>
      <select class="fsel" [(ngModel)]="filterCategory" (ngModelChange)="loadTickets()">
        <option value="">All Categories</option>
        <option value="Order">Order</option>
        <option value="Payment">Payment</option>
        <option value="Delivery">Delivery</option>
        <option value="Product">Product</option>
        <option value="Other">Other</option>
      </select>
    </div>

    <!-- Tickets Table -->
    <div class="table-card">
      @if (loading()) {
        <div class="loading-rows">@for (i of [1,2,3]; track i) { <div class="skeleton"></div> }</div>
      } @else if (tickets().length === 0) {
        <div class="empty"><span>🎫</span><p>No tickets found</p></div>
      } @else {
        <table>
          <thead><tr><th>TICKET</th><th>CUSTOMER</th><th>STATUS</th><th>PRIORITY</th><th>CATEGORY</th><th>DATE</th><th></th></tr></thead>
          <tbody>
            @for (t of tickets(); track t.id; let i = $index) {
              <tr [class.row-alt]="i % 2 === 1">
                <td>
                  <p class="ticket-id">#{{ t.id.slice(0,8).toUpperCase() }}</p>
                  <p class="ticket-subject">{{ t.subject }}</p>
                </td>
                <td>
                  <p class="cname">{{ t.customerName }}</p>
                  <p class="cemail">{{ t.customerEmail }}</p>
                </td>
                <td><span class="chip" [class]="'st-' + t.status.toLowerCase()">{{ t.status }}</span></td>
                <td><span class="chip" [class]="'pr-' + t.priority.toLowerCase()">{{ t.priority }}</span></td>
                <td class="muted">{{ t.category }}</td>
                <td class="muted">{{ t.createdAt | date:'dd MMM' }}</td>
                <td><button class="btn-view" (click)="openTicket(t)">View →</button></td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  }

  <!-- Ticket Detail -->
  @if (selectedTicket()) {
    <div class="detail-grid">
      <!-- Chat -->
      <div class="chat-card">
        <div class="chat-head">
          <p class="ticket-id">#{{ selectedTicket()!.id.slice(0,8).toUpperCase() }}</p>
          <h2 class="chat-title">{{ selectedTicket()!.subject }}</h2>
          <p class="cemail">{{ selectedTicket()!.customerName }} · {{ selectedTicket()!.customerEmail }}</p>
        </div>
        <div #msgContainer class="messages">
          @for (msg of messages(); track msg.id) {
            <div class="msg-row" [class.staff]="msg.isStaff">
              <div class="bubble" [class.staff-bubble]="msg.isStaff" [class.user-bubble]="!msg.isStaff">
                <p class="msg-sender">{{ msg.isStaff ? msg.senderName + ' (Staff)' : msg.senderName }}</p>
                <p class="msg-text">{{ msg.message }}</p>
                <p class="msg-time">{{ msg.createdAt | date:'dd MMM, HH:mm' }}</p>
              </div>
            </div>
          }
          @if (messages().length === 0) {
            <div class="empty-chat">No messages yet</div>
          }
        </div>
        <div class="reply-bar">
          <input class="reply-input" [(ngModel)]="replyText" (keyup.enter)="sendReply()" placeholder="Reply to customer..." />
          <button class="btn-send" (click)="sendReply()" [disabled]="!replyText.trim() || sending()">
            {{ sending() ? '...' : 'Send' }}
          </button>
        </div>
      </div>

      <!-- Info Panel -->
      <div class="info-panel">
        <div class="info-card">
          <h3 class="info-title">Ticket Details</h3>
          <div class="field-group"><label class="field-label">Status</label>
            <select class="field" [(ngModel)]="editStatus">
              <option value="Open">Open</option><option value="InProgress">In Progress</option>
              <option value="Resolved">Resolved</option><option value="Closed">Closed</option>
            </select>
          </div>
          <div class="field-group" style="margin-top:12px"><label class="field-label">Priority</label>
            <select class="field" [(ngModel)]="editPriority">
              <option value="Low">Low</option><option value="Medium">Medium</option><option value="High">High</option>
            </select>
          </div>
          <button class="btn-update" (click)="updateStatus()" [disabled]="updating()">
            {{ updating() ? 'Saving...' : '💾 Update Ticket' }}
          </button>
        </div>
        <div class="info-card">
          <h3 class="info-title">Customer Info</h3>
          <p class="cname">{{ selectedTicket()!.customerName }}</p>
          <p class="cemail">{{ selectedTicket()!.customerEmail }}</p>
          <div class="info-rows">
            <div class="info-row"><span>Category</span><span>{{ selectedTicket()!.category }}</span></div>
            <div class="info-row"><span>Created</span><span>{{ selectedTicket()!.createdAt | date:'dd MMM yyyy' }}</span></div>
            @if (selectedTicket()!.resolvedAt) {
              <div class="info-row"><span>Resolved</span><span class="green">{{ selectedTicket()!.resolvedAt | date:'dd MMM yyyy' }}</span></div>
            }
          </div>
        </div>
      </div>
    </div>
  }
</div>
  `,
  styles: [`
    * { box-sizing:border-box; }
    .page { padding:28px; color:var(--adm-text); min-height:100vh; background:var(--adm-bg); }
    .page-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:28px; }
    .page-title { font-size:26px; font-weight:800; margin:0; background:linear-gradient(135deg,#38bdf8,#818cf8); -webkit-background-clip:text; -webkit-text-fill-color:transparent; }
    .page-sub { margin:4px 0 0; font-size:13px; color:var(--adm-text2); }
    .btn-back { background:var(--adm-card); color:var(--adm-text2); border:1px solid var(--adm-border2); padding:9px 18px; border-radius:8px; cursor:pointer; font-size:13px; font-weight:500; transition:all .2s; }
    .btn-back:hover { background:#334155; color:var(--adm-text); }

    .stats { display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin-bottom:24px; }
    .stat-card { display:flex; align-items:center; gap:14px; border-radius:14px; padding:18px; border:1px solid rgba(255,255,255,.06); }
    .c1{background:var(--adm-s1);} .c2{background:var(--adm-s1);} .c3{background:var(--adm-s3);} .c4{background:var(--adm-s2);}
    .stat-icon{font-size:26px;} .stat-val{font-size:28px;font-weight:800;color:var(--adm-text);line-height:1;} .stat-lbl{font-size:12px;color:var(--adm-text3);margin-top:4px;}

    .filters { display:flex; gap:12px; margin-bottom:20px; }
    .fsel { background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text); padding:10px 14px; border-radius:8px; font-size:14px; }
    .fsel:focus { outline:none; border-color:#38bdf8; }

    .table-card { background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); overflow:hidden; }
    table { width:100%; border-collapse:collapse; }
    thead tr { background:var(--adm-card2); }
    th { padding:12px 16px; text-align:left; font-size:11px; color:var(--adm-text2); font-weight:700; letter-spacing:.06em; border-bottom:1px solid var(--adm-border); }
    td { padding:11px 16px; font-size:13px; border-bottom:1px solid var(--adm-border); vertical-align:middle; color:var(--adm-text); }
    tr.row-alt td { background:var(--adm-row-alt); }
    tbody tr:hover td { background:var(--adm-row-hover); }

    .ticket-id { font-family:monospace; font-size:11px; color:var(--adm-text3); margin:0; }
    .ticket-subject { font-weight:600; color:var(--adm-text); margin:2px 0 0; font-size:13px; max-width:220px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
    .cname { font-weight:500; color:var(--adm-text); margin:0; font-size:13px; }
    .cemail { color:var(--adm-text3); font-size:11px; margin:2px 0 0; }
    .muted { color:var(--adm-text2); font-size:12px; }

    .chip { padding:3px 10px; border-radius:20px; font-size:11px; font-weight:600; }
    .st-open{background:rgba(59,130,246,.15);color:#60a5fa;} .st-inprogress{background:rgba(251,191,36,.15);color:#fbbf24;} .st-resolved{background:rgba(34,197,94,.15);color:#4ade80;} .st-closed{background:rgba(100,116,139,.15);color:var(--adm-text2);}
    .pr-high{background:rgba(239,68,68,.15);color:#f87171;} .pr-medium{background:rgba(251,191,36,.15);color:#fbbf24;} .pr-low{background:rgba(100,116,139,.15);color:var(--adm-text2);}

    .btn-view { background:linear-gradient(135deg,#2563eb,#3b82f6); color:#fff; border:none; padding:6px 14px; border-radius:7px; cursor:pointer; font-size:12px; font-weight:600; transition:all .2s; white-space:nowrap; }
    .btn-view:hover { transform:translateY(-1px); box-shadow:0 4px 12px rgba(59,130,246,.4); }

    .empty { text-align:center; padding:60px; display:flex; flex-direction:column; align-items:center; gap:8px; color:var(--adm-text3); }
    .empty span { font-size:40px; } .empty p { margin:0; font-size:15px; }
    .loading-rows { padding:16px; display:flex; flex-direction:column; gap:10px; }
    .skeleton { height:52px; background:var(--adm-card); border-radius:8px; animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.5} }

    /* Detail */
    .detail-grid { display:grid; grid-template-columns:1fr 300px; gap:20px; }
    .chat-card { background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); overflow:hidden; display:flex; flex-direction:column; }
    .chat-head { padding:18px 20px; border-bottom:1px solid var(--adm-border); background:linear-gradient(135deg,var(--adm-card2),var(--adm-card)); }
    .chat-title { font-size:16px; font-weight:700; color:var(--adm-text); margin:4px 0 2px; }
    .messages { flex:1; min-height:320px; max-height:420px; overflow-y:auto; padding:16px 20px; display:flex; flex-direction:column; gap:12px; background:var(--adm-bg); }
    .msg-row { display:flex; }
    .msg-row.staff { justify-content:flex-end; }
    .bubble { max-width:70%; padding:12px 16px; border-radius:14px; }
    .staff-bubble { background:linear-gradient(135deg,#166534,#15803d); border-radius:14px 14px 4px 14px; }
    .user-bubble { background:var(--adm-card); border:1px solid var(--adm-border2); border-radius:14px 14px 14px 4px; }
    .msg-sender { font-size:11px; font-weight:600; margin:0 0 4px; color:rgba(255,255,255,.6); }
    .msg-text { font-size:13.5px; margin:0 0 6px; color:var(--adm-text); line-height:1.5; }
    .msg-time { font-size:10px; margin:0; color:rgba(255,255,255,.4); }
    .empty-chat { text-align:center; color:var(--adm-text3); font-size:14px; padding:40px; }
    .reply-bar { display:flex; gap:10px; padding:14px 20px; border-top:1px solid var(--adm-border); background:var(--adm-card2); }
    .reply-input { flex:1; background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text); padding:10px 14px; border-radius:8px; font-size:14px; }
    .reply-input:focus { outline:none; border-color:#38bdf8; }
    .btn-send { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:10px 20px; border-radius:8px; font-weight:700; font-size:14px; cursor:pointer; }
    .btn-send:disabled { opacity:.5; cursor:not-allowed; }

    .info-panel { display:flex; flex-direction:column; gap:16px; }
    .info-card { background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); padding:18px 20px; }
    .info-title { font-size:13px; font-weight:700; color:var(--adm-text2); text-transform:uppercase; letter-spacing:.06em; margin:0 0 14px; }
    .field-group { display:flex; flex-direction:column; gap:5px; }
    .field-label { font-size:11px; font-weight:600; color:var(--adm-text2); text-transform:uppercase; letter-spacing:.05em; }
    .field { background:var(--adm-input-bg); border:1px solid var(--adm-border); color:var(--adm-text); padding:9px 12px; border-radius:8px; font-size:13px; width:100%; }
    .field:focus { outline:none; border-color:#38bdf8; }
    .btn-update { width:100%; margin-top:14px; background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:10px; border-radius:8px; font-weight:700; font-size:14px; cursor:pointer; box-shadow:0 4px 15px rgba(34,197,94,.25); }
    .btn-update:disabled { opacity:.5; cursor:not-allowed; }
    .info-rows { margin-top:12px; padding-top:12px; border-top:1px solid var(--adm-border); display:flex; flex-direction:column; gap:8px; }
    .info-row { display:flex; justify-content:space-between; font-size:12px; }
    .info-row span:first-child { color:var(--adm-text3); }
    .info-row span:last-child { color:var(--adm-text2); }
    .green { color:#4ade80 !important; }
  `]
})
export class AdminSupport implements OnInit, OnDestroy, AfterViewChecked {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = `${environment.apiUrl}/api/v1`;
  private hub?: signalR.HubConnection;
  private shouldScroll = false;

  @ViewChild('msgContainer') msgContainer?: ElementRef<HTMLDivElement>;

  tickets = signal<SupportTicket[]>([]);
  selectedTicket = signal<SupportTicket | null>(null);
  messages = signal<SupportMessage[]>([]);
  loading = signal(true);
  sending = signal(false);
  updating = signal(false);

  filterStatus = ''; filterPriority = ''; filterCategory = '';
  replyText = ''; editStatus = 'Open'; editPriority = 'Medium';

  countByStatus = (s: string) => this.tickets().filter(t => t.status === s).length;

  ngOnInit() {
    this.loadTickets();
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

  ngOnDestroy() { this.hub?.stop(); }

  loadTickets() {
    this.loading.set(true);
    const params: Record<string, string> = {};
    if (this.filterStatus) params['status'] = this.filterStatus;
    if (this.filterPriority) params['priority'] = this.filterPriority;
    if (this.filterCategory) params['category'] = this.filterCategory;
    this.http.get<SupportTicket[]>(`${this.api}/support/tickets`, { params }).subscribe({
      next: t => { this.tickets.set(t); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  loadTicketById(id: string) {
    this.http.get<{ ticket: SupportTicket; messages: SupportMessage[] }>(`${this.api}/support/tickets/${id}`).subscribe({
      next: res => {
        this.selectedTicket.set(res.ticket);
        this.editStatus = res.ticket.status;
        this.editPriority = res.ticket.priority;
        this.messages.set(res.messages);
        this.shouldScroll = true;
        this.connectHub(id);
      }
    });
  }

  openTicket(ticket: SupportTicket) {
    this.router.navigate(['/admin/support', ticket.id]);
    this.loadTicketById(ticket.id);
  }

  connectHub(ticketId: string) {
    const token = this.auth.getAccessToken();
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/support`, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect().build();
    this.hub.on('newMessage', (msg: SupportMessage) => { this.messages.update(m => [...m, msg]); this.shouldScroll = true; });
    this.hub.on('ticketUpdated', (update: Partial<SupportTicket>) => { this.selectedTicket.update(t => t ? { ...t, ...update } : t); });
    this.hub.start().then(() => this.hub!.invoke('JoinTicket', ticketId)).catch(console.error);
  }

  sendReply() {
    if (!this.replyText.trim() || !this.selectedTicket()) return;
    this.sending.set(true);
    const id = this.selectedTicket()!.id;
    this.http.post<SupportMessage>(`${this.api}/support/tickets/${id}/messages`, { message: this.replyText }).subscribe({
      next: msg => { this.messages.update(m => [...m, msg]); this.replyText = ''; this.sending.set(false); this.shouldScroll = true; },
      error: () => this.sending.set(false)
    });
  }

  updateStatus() {
    if (!this.selectedTicket()) return;
    this.updating.set(true);
    const id = this.selectedTicket()!.id;
    this.http.patch(`${this.api}/support/tickets/${id}/status`, { status: this.editStatus, priority: this.editPriority }).subscribe({
      next: () => {
        this.selectedTicket.update(t => t ? { ...t, status: this.editStatus as any, priority: this.editPriority as any } : t);
        this.tickets.update(l => l.map(t => t.id === id ? { ...t, status: this.editStatus as any, priority: this.editPriority as any } : t));
        this.updating.set(false);
      },
      error: () => this.updating.set(false)
    });
  }
}
