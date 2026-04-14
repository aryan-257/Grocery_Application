"""
FreshMart — Diagram Generator
Generates all PNG diagrams used by generate_project_doc.py.
Output folder: diagrams/
Run: python generate_diagrams.py
"""
import os
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
import matplotlib.patheffects as pe
import numpy as np

os.makedirs('diagrams', exist_ok=True)

# ── Colour palette ────────────────────────────────────────────────────────────
C = {
    'dark_green':  '#1B5E20',
    'mid_green':   '#2E7D32',
    'light_green': '#4CAF50',
    'pale_green':  '#C8E6C9',
    'mint':        '#E8F5E9',
    'white':       '#FFFFFF',
    'header_bg':   '#1B5E20',
    'row_even':    '#F1F8E9',
    'row_odd':     '#FFFFFF',
    'pk_yellow':   '#FFF9C4',
    'fk_blue':     '#E3F2FD',
    'border':      '#2E7D32',
    'text_dark':   '#1A1A1A',
    'text_white':  '#FFFFFF',
    'text_green':  '#1B5E20',
    'arrow':       '#1B5E20',
    'shadow':      '#BDBDBD',
    'orange':      '#FF6F00',
    'blue':        '#1565C0',
    'purple':      '#6A1B9A',
    'teal':        '#00695C',
    'red':         '#B71C1C',
    'amber':       '#F57F17',
}

def save(fig, name):
    fig.savefig(f'diagrams/{name}', dpi=150, bbox_inches='tight',
                facecolor=fig.get_facecolor())
    plt.close(fig)
    print(f'  ✓ diagrams/{name}')

def rounded_box(ax, x, y, w, h, color, text, fontsize=9, text_color='white',
                bold=False, radius=0.04, zorder=3, alpha=1.0):
    box = FancyBboxPatch((x - w/2, y - h/2), w, h,
                         boxstyle=f'round,pad=0,rounding_size={radius}',
                         facecolor=color, edgecolor=C['border'],
                         linewidth=1.2, zorder=zorder, alpha=alpha)
    ax.add_patch(box)
    ax.text(x, y, text, ha='center', va='center', fontsize=fontsize,
            color=text_color, fontweight='bold' if bold else 'normal',
            zorder=zorder+1, wrap=True,
            multialignment='center')

def arrow(ax, x1, y1, x2, y2, label='', color=None, lw=1.5, style='->', zorder=2):
    color = color or C['arrow']
    ax.annotate('', xy=(x2, y2), xytext=(x1, y1),
                arrowprops=dict(arrowstyle=style, color=color,
                                lw=lw, connectionstyle='arc3,rad=0.0'),
                zorder=zorder)
    if label:
        mx, my = (x1+x2)/2, (y1+y2)/2
        ax.text(mx+0.02, my+0.02, label, fontsize=7, color=color,
                ha='center', va='bottom', zorder=zorder+1,
                bbox=dict(boxstyle='round,pad=0.15', facecolor='white',
                          edgecolor='none', alpha=0.8))


# ══════════════════════════════════════════════════════════════════════════════
# 01 — SYSTEM ARCHITECTURE
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 01_architecture.png ...')
fig, ax = plt.subplots(figsize=(14, 9))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 14); ax.set_ylim(0, 9)
ax.axis('off')
ax.set_title('FreshMart — System Architecture', fontsize=16, fontweight='bold',
             color=C['dark_green'], pad=12)

# Frontend
rounded_box(ax, 7, 8.3, 5.5, 0.7, C['dark_green'],
            'Angular 21 SPA  (Browser :4200 / Nginx :80)', fontsize=10, bold=True)

# API Gateway
rounded_box(ax, 7, 7.2, 5.5, 0.7, C['mid_green'],
            'API Gateway — YARP Reverse Proxy  (:8080)\nJWT Validation · CORS · CorrelationId · Route Forwarding',
            fontsize=8.5, bold=False)
arrow(ax, 7, 7.95, 7, 7.55, label='HTTP / WebSocket')

# Six services
services = [
    (1.2,  5.8, 'AuthService\n:5001',         C['dark_green']),
    (3.4,  5.8, 'ProductService\n:5002',       C['mid_green']),
    (5.6,  5.8, 'OrderService\n:5003',         C['dark_green']),
    (7.8,  5.8, 'PaymentService\n:5004',       C['mid_green']),
    (10.0, 5.8, 'NotificationService\n:5005',  C['dark_green']),
    (12.2, 5.8, 'SupportService\n:5006',       C['mid_green']),
]
for sx, sy, label, col in services:
    rounded_box(ax, sx, sy, 1.9, 0.9, col, label, fontsize=8, bold=True)
    arrow(ax, 7, 6.85, sx, 6.25, color=C['light_green'], lw=1.2)

# Databases
dbs = [
    (1.2,  4.5, 'Auth DB',         C['pale_green']),
    (3.4,  4.5, 'Product DB',      C['pale_green']),
    (5.6,  4.5, 'Order DB',        C['pale_green']),
    (7.8,  4.5, 'Payment DB',      C['pale_green']),
    (10.0, 4.5, 'Notification DB', C['pale_green']),
    (12.2, 4.5, 'Support DB',      C['pale_green']),
]
for dx, dy, label, col in dbs:
    rounded_box(ax, dx, dy, 1.9, 0.7, col, label, fontsize=7.5,
                text_color=C['dark_green'], bold=True)
    arrow(ax, dx, dy+0.7/2+0.35, dx, dy+0.35+0.02, color=C['mid_green'], lw=1.0)

# SQL Server banner
ax.add_patch(FancyBboxPatch((0.1, 4.05), 13.8, 0.25,
             boxstyle='round,pad=0.02', facecolor=C['pale_green'],
             edgecolor=C['border'], linewidth=0.8, alpha=0.5))
ax.text(7, 4.17, 'SQL Server 2022 — 6 isolated databases (one per service)',
        ha='center', va='center', fontsize=8, color=C['dark_green'], style='italic')

# RabbitMQ
rounded_box(ax, 7, 3.1, 4.0, 0.7, C['orange'],
            'RabbitMQ + MassTransit\nOrderPlacedEvent · OrderStatusChangedEvent · PaymentCompletedEvent',
            fontsize=8, bold=True, text_color='white')
# Arrows to/from RabbitMQ
for sx in [5.6, 7.8]:
    arrow(ax, sx, 5.35, 7, 3.45, color=C['orange'], lw=1.0, style='->')
for sx in [3.4, 10.0]:
    arrow(ax, 7, 3.45, sx, 5.35, color=C['orange'], lw=1.0, style='->')

# External integrations
ext = [
    (2.0, 1.8, 'Razorpay\n(Payments)', C['blue']),
    (7.0, 1.8, 'Gmail SMTP\n(Emails)',  C['teal']),
    (12.0,1.8, 'Google OAuth2\n(Login)',C['red']),
]
for ex, ey, label, col in ext:
    rounded_box(ax, ex, ey, 2.2, 0.8, col, label, fontsize=8.5, bold=True)

arrow(ax, 7.8, 5.35, 2.0, 2.2, color=C['blue'],  lw=1.0, label='Razorpay API')
arrow(ax, 10.0,5.35, 7.0, 2.2, color=C['teal'],  lw=1.0, label='SMTP')
arrow(ax, 1.2, 5.35, 12.0,2.2, color=C['red'],   lw=1.0, label='OAuth2')

# Legend
legend_items = [
    mpatches.Patch(color=C['dark_green'],  label='Microservice'),
    mpatches.Patch(color=C['pale_green'],  label='Database'),
    mpatches.Patch(color=C['orange'],      label='Message Broker'),
    mpatches.Patch(color=C['blue'],        label='External Service'),
]
ax.legend(handles=legend_items, loc='lower left', fontsize=8,
          framealpha=0.9, edgecolor=C['border'])

save(fig, '01_architecture.png')


# ══════════════════════════════════════════════════════════════════════════════
# 02 — LEVEL 0 DFD (CONTEXT DIAGRAM)
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 02_dfd_level0.png ...')
fig, ax = plt.subplots(figsize=(13, 9))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 13); ax.set_ylim(0, 9)
ax.axis('off')
ax.set_title('FreshMart — Level 0 DFD (Context Diagram)', fontsize=15,
             fontweight='bold', color=C['dark_green'], pad=12)

# Central system
cx, cy = 6.5, 4.5
rounded_box(ax, cx, cy, 3.8, 2.0, C['dark_green'],
            'FreshMart\nPlatform\n(API Gateway + 6 Microservices)',
            fontsize=11, bold=True, radius=0.08)

# External entities (rectangles)
entities = [
    (1.2,  7.8, 'Customer',        C['mid_green']),
    (6.5,  8.3, 'Store Manager',   C['mid_green']),
    (11.8, 7.8, 'Admin',           C['mid_green']),
    (1.2,  1.2, 'Delivery Driver', C['mid_green']),
    (6.5,  0.5, 'Razorpay',        C['blue']),
    (11.8, 1.2, 'Gmail SMTP',      C['teal']),
    (11.8, 4.5, 'Google OAuth2',   C['red']),
]
for ex, ey, label, col in entities:
    rounded_box(ax, ex, ey, 2.0, 0.75, col, label, fontsize=9.5, bold=True, radius=0.03)

def dbl_arrow(ax, x1, y1, x2, y2, label_in, label_out, col='#1B5E20'):
    mx, my = (x1+x2)/2, (y1+y2)/2
    dx, dy = x2-x1, y2-y1
    length = (dx**2+dy**2)**0.5
    nx, ny = -dy/length*0.12, dx/length*0.12
    ax.annotate('', xy=(x2+nx, y2+ny), xytext=(x1+nx, y1+ny),
                arrowprops=dict(arrowstyle='->', color=col, lw=1.4))
    ax.annotate('', xy=(x1-nx, y1-ny), xytext=(x2-nx, y2-ny),
                arrowprops=dict(arrowstyle='->', color=col, lw=1.4))
    ax.text(mx+nx*2.5, my+ny*2.5, label_in,  fontsize=6.5, color=col,
            ha='center', va='center',
            bbox=dict(boxstyle='round,pad=0.1', facecolor='white', edgecolor='none', alpha=0.85))
    ax.text(mx-nx*2.5, my-ny*2.5, label_out, fontsize=6.5, color=col,
            ha='center', va='center',
            bbox=dict(boxstyle='round,pad=0.1', facecolor='white', edgecolor='none', alpha=0.85))

dbl_arrow(ax, 1.9, 7.45, cx-1.9, cy+0.9,
          'Login, Cart, Orders, Reviews, Support',
          'JWT, Products, Order Status, Notifications')
dbl_arrow(ax, 6.5, 7.95, cx, cy+1.0,
          'Product updates, Stock, Order status',
          'Product list, Orders, Alerts')
dbl_arrow(ax, 11.1, 7.45, cx+1.9, cy+0.9,
          'User mgmt, Coupons, Role changes',
          'User list, Stats, All orders')
dbl_arrow(ax, 1.9, 1.55, cx-1.9, cy-0.9,
          'Delivery status updates',
          'Assigned orders, Delivery details')
dbl_arrow(ax, 6.5, 0.88, cx, cy-1.0,
          'Payment ID + Signature (webhook)',
          'Create payment order request')
dbl_arrow(ax, 11.1, 1.55, cx+1.9, cy-0.9,
          'Email delivery status',
          'HTML transactional emails')
dbl_arrow(ax, 11.45, 4.5, cx+1.9, cy,
          'User profile (name, email, sub)',
          'Token verification request')

save(fig, '02_dfd_level0.png')


# ══════════════════════════════════════════════════════════════════════════════
# 03 — PROJECT STRUCTURE TREE
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 03_project_structure.png ...')

lines = [
    ('FreshMart-main/',                                    0, True),
    ('├── docker-compose.yml',                             1, False),
    ('├── FreshMart-main.sln',                             1, False),
    ('├── generate_diagrams.py',                           1, False),
    ('├── generate_project_doc.py',                        1, False),
    ('├── Frontend/',                                      1, True),
    ('│   ├── src/app/',                                   2, True),
    ('│   │   ├── core/  (guards, interceptors, services, models)', 3, False),
    ('│   │   ├── pages/ (home, products, cart, checkout, orders,', 3, False),
    ('│   │   │           profile, support, admin, delivery, ...)', 3, False),
    ('│   │   └── shared/ (navbar, product-card, search-bar)',      3, False),
    ('│   ├── Dockerfile  (Node 20 build → Nginx Alpine serve)',    2, False),
    ('│   └── nginx.conf',                                 2, False),
    ('└── Microservices/',                                 1, True),
    ('    ├── ApiGateway/  (:8080)',                        2, True),
    ('    │   ├── Program.cs',                             3, False),
    ('    │   └── appsettings.json  (YARP routes)',        3, False),
    ('    ├── AuthService/  (:5001)',                       2, True),
    ('    │   ├── AppUser.cs',                             3, False),
    ('    │   ├── AuthController.cs',                      3, False),
    ('    │   ├── UsersController.cs',                     3, False),
    ('    │   └── JwtService.cs',                          3, False),
    ('    ├── ProductService/  (:5002)',                    2, True),
    ('    │   ├── Product.cs  Category.cs  Review.cs',     3, False),
    ('    │   ├── OrderProjection.cs  (local read-model)', 3, False),
    ('    │   └── Consumers/  (MassTransit)',               3, False),
    ('    ├── OrderService/  (:5003)',                      2, True),
    ('    │   ├── Order.cs  Cart.cs  Coupon.cs',           3, False),
    ('    │   ├── OrdersController.cs  CartController.cs', 3, False),
    ('    │   └── Messaging/  (MassTransit publishers)',   3, False),
    ('    ├── PaymentService/  (:5004)',                    2, True),
    ('    │   ├── Models/Payment.cs',                      3, False),
    ('    │   ├── PaymentController.cs',                   3, False),
    ('    │   └── Services/  (Razorpay SDK)',               3, False),
    ('    ├── NotificationService/  (:5005)',               2, True),
    ('    │   ├── Notification.cs',                        3, False),
    ('    │   ├── NotificationHub.cs  (SignalR)',           3, False),
    ('    │   ├── EmailService.cs  (MailKit)',              3, False),
    ('    │   └── Consumers/  (MassTransit)',               3, False),
    ('    ├── SupportService/  (:5006)',                    2, True),
    ('    │   ├── SupportTicket.cs  SupportMessage.cs',    3, False),
    ('    │   └── SupportHub.cs  (SignalR)',                3, False),
    ('    └── SharedModels/',                              2, True),
    ('        └── Events.cs  (OrderPlacedEvent,',          3, False),
    ('                        OrderStatusChangedEvent,',   3, False),
    ('                        PaymentCompletedEvent)',      3, False),
]

fig_h = max(10, len(lines) * 0.28 + 1.5)
fig, ax = plt.subplots(figsize=(13, fig_h))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 13); ax.set_ylim(0, fig_h)
ax.axis('off')
ax.set_title('FreshMart — Project Folder Structure', fontsize=15,
             fontweight='bold', color=C['dark_green'], pad=10)

y = fig_h - 1.2
row_h = (fig_h - 1.5) / len(lines)
indent_w = 0.38

for text, level, is_dir in lines:
    x = 0.3 + level * indent_w
    bg = C['pale_green'] if is_dir else C['white']
    fc = C['dark_green'] if is_dir else C['text_dark']
    fw = 'bold' if is_dir else 'normal'
    ax.text(x, y, text, fontsize=8.2, color=fc, fontweight=fw,
            va='center', fontfamily='monospace')
    y -= row_h

save(fig, '03_project_structure.png')


# ══════════════════════════════════════════════════════════════════════════════
# ER DIAGRAM HELPER
# ══════════════════════════════════════════════════════════════════════════════
def draw_er_table(ax, x, y, title, fields, width=3.2, row_h=0.32, title_h=0.42):
    """
    fields: list of (name, type, tag)  tag = 'PK' | 'FK' | ''
    Returns bottom-y of the table.
    """
    total_h = title_h + len(fields) * row_h
    # Shadow
    ax.add_patch(FancyBboxPatch((x+0.06, y-total_h-0.06), width, total_h,
                 boxstyle='round,pad=0.02', facecolor=C['shadow'],
                 edgecolor='none', zorder=1))
    # Title bar
    ax.add_patch(FancyBboxPatch((x, y-title_h), width, title_h,
                 boxstyle='round,pad=0.02', facecolor=C['header_bg'],
                 edgecolor=C['border'], linewidth=1.5, zorder=2))
    ax.text(x + width/2, y - title_h/2, title,
            ha='center', va='center', fontsize=9, fontweight='bold',
            color=C['text_white'], zorder=3)
    # Fields
    for i, (fname, ftype, tag) in enumerate(fields):
        fy = y - title_h - i * row_h
        bg = C['pk_yellow'] if tag == 'PK' else (C['fk_blue'] if tag == 'FK' else
             (C['row_even'] if i % 2 == 0 else C['row_odd']))
        ax.add_patch(FancyBboxPatch((x, fy - row_h), width, row_h,
                     boxstyle='round,pad=0.01', facecolor=bg,
                     edgecolor=C['border'], linewidth=0.6, zorder=2))
        # Tag badge
        if tag:
            badge_col = C['amber'] if tag == 'PK' else C['blue']
            ax.add_patch(FancyBboxPatch((x+0.04, fy-row_h+0.04), 0.32, row_h-0.08,
                         boxstyle='round,pad=0.01', facecolor=badge_col,
                         edgecolor='none', zorder=3))
            ax.text(x+0.20, fy-row_h/2, tag, ha='center', va='center',
                    fontsize=6, fontweight='bold', color='white', zorder=4)
        ax.text(x+0.42, fy-row_h/2, fname, ha='left', va='center',
                fontsize=7.8, color=C['text_dark'], zorder=3)
        ax.text(x+width-0.08, fy-row_h/2, ftype, ha='right', va='center',
                fontsize=7, color='#555555', style='italic', zorder=3)
    return y - total_h

def er_arrow(ax, x1, y1, x2, y2, label='1:N', color=None):
    color = color or C['mid_green']
    ax.annotate('', xy=(x2, y2), xytext=(x1, y1),
                arrowprops=dict(arrowstyle='->', color=color, lw=1.5,
                                connectionstyle='arc3,rad=0.1'))
    mx, my = (x1+x2)/2, (y1+y2)/2
    ax.text(mx+0.08, my+0.08, label, fontsize=7.5, color=color,
            fontweight='bold',
            bbox=dict(boxstyle='round,pad=0.15', facecolor='white',
                      edgecolor='none', alpha=0.9))


# ══════════════════════════════════════════════════════════════════════════════
# 04 — ER: AuthService
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 04_er_auth.png ...')
fig, ax = plt.subplots(figsize=(7, 8))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 7); ax.set_ylim(0, 8)
ax.axis('off')
ax.set_title('ER Diagram — AuthService  (FreshMart_Auth)', fontsize=13,
             fontweight='bold', color=C['dark_green'], pad=10)

draw_er_table(ax, 1.5, 7.5, 'AppUser', [
    ('Id',                 'Guid',      'PK'),
    ('Email',              'string',    ''),
    ('PasswordHash',       'string',    ''),
    ('FirstName',          'string',    ''),
    ('LastName',           'string',    ''),
    ('Role',               'string',    ''),
    ('PhoneNumber',        'string?',   ''),
    ('IsActive',           'bool',      ''),
    ('CreatedAt',          'DateTime',  ''),
    ('RefreshToken',       'string?',   ''),
    ('RefreshTokenExpiry', 'DateTime?', ''),
    ('GoogleId',           'string?',   ''),
], width=4.0)

# Role enum note
ax.text(1.5, 1.5,
        'Role values:  Admin  |  StoreManager  |  DeliveryDriver  |  Customer\n'
        'Auth method:  Email+Password  |  Google OAuth2\n'
        'Token:  JWT HS256 (1h access)  +  Refresh Token (7 days)',
        fontsize=8, color=C['dark_green'],
        bbox=dict(boxstyle='round,pad=0.4', facecolor=C['pale_green'],
                  edgecolor=C['border'], linewidth=1))

save(fig, '04_er_auth.png')


# ══════════════════════════════════════════════════════════════════════════════
# 05 — ER: ProductService
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 05_er_product.png ...')
fig, ax = plt.subplots(figsize=(14, 11))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 14); ax.set_ylim(0, 11)
ax.axis('off')
ax.set_title('ER Diagram — ProductService  (FreshMart_Product)', fontsize=13,
             fontweight='bold', color=C['dark_green'], pad=10)

# Category
draw_er_table(ax, 0.4, 10.5, 'Category', [
    ('Id',               'Guid',    'PK'),
    ('Name',             'string',  ''),
    ('Description',      'string?', ''),
    ('ImageUrl',         'string?', ''),
    ('ParentCategoryId', 'Guid?',   'FK'),
], width=3.4)

# Self-ref arrow
ax.annotate('', xy=(0.4+3.4, 10.5-0.42-1*0.32-0.16),
            xytext=(0.4+3.4+0.5, 10.5-0.42-1*0.32-0.16),
            arrowprops=dict(arrowstyle='->', color=C['mid_green'], lw=1.3,
                            connectionstyle='arc3,rad=-0.5'))
ax.text(4.5, 9.5, 'self-ref\n(parent)', fontsize=7, color=C['mid_green'],
        ha='center', style='italic')

# Product
draw_er_table(ax, 0.4, 7.8, 'Product', [
    ('Id',              'Guid',    'PK'),
    ('Name',            'string',  ''),
    ('Description',     'string',  ''),
    ('Price',           'decimal', ''),
    ('Sku',             'string',  ''),
    ('ImageUrl',        'string',  ''),
    ('CategoryId',      'Guid',    'FK'),
    ('StockQuantity',   'int',     ''),
    ('IsActive',        'bool',    ''),
    ('AverageRating',   'double',  ''),
    ('Brand',           'string?', ''),
    ('Unit',            'string?', ''),
    ('DiscountPercent', 'decimal', ''),
    ('CreatedAt',       'DateTime',''),
], width=3.4)

# Category → Product
er_arrow(ax, 0.4+3.4/2, 10.5-0.42-5*0.32,
             0.4+3.4/2, 7.8,
         label='1:N', color=C['mid_green'])

# Review
draw_er_table(ax, 5.0, 7.8, 'Review', [
    ('Id',           'Guid',    'PK'),
    ('ProductId',    'Guid',    'FK'),
    ('CustomerId',   'Guid',    ''),
    ('CustomerName', 'string',  ''),
    ('Rating',       'int 1-5', ''),
    ('Comment',      'string',  ''),
    ('CreatedAt',    'DateTime',''),
], width=3.4)

# Product → Review
er_arrow(ax, 0.4+3.4, 7.8-0.42-1*0.32-0.16,
             5.0,      7.8-0.42-1*0.32-0.16,
         label='1:N')

# OrderProjection
draw_er_table(ax, 9.5, 10.5, 'OrderProjection\n(read-model)', [
    ('Id',         'Guid',   'PK'),
    ('CustomerId', 'Guid',   ''),
    ('Status',     'string', ''),
], width=3.8)

# OrderItemProjection
draw_er_table(ax, 9.5, 7.8, 'OrderItemProjection\n(read-model)', [
    ('Id',        'Guid', 'PK'),
    ('OrderId',   'Guid', 'FK'),
    ('ProductId', 'Guid', ''),
], width=3.8)

er_arrow(ax, 9.5+3.8/2, 10.5-0.42-3*0.32,
             9.5+3.8/2, 7.8,
         label='1:N', color=C['teal'])

ax.text(9.5, 6.5,
        '* OrderProjection is a local read-model\n'
        '  populated via MassTransit OrderPlacedEvent.\n'
        '  Used to gate product reviews (purchase check).',
        fontsize=7.5, color=C['teal'], style='italic',
        bbox=dict(boxstyle='round,pad=0.3', facecolor='#E0F2F1',
                  edgecolor=C['teal'], linewidth=0.8))

save(fig, '05_er_product.png')


# ══════════════════════════════════════════════════════════════════════════════
# 06 — ER: OrderService
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 06_er_order.png ...')
fig, ax = plt.subplots(figsize=(15, 12))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 15); ax.set_ylim(0, 12)
ax.axis('off')
ax.set_title('ER Diagram — OrderService  (FreshMart_Order)', fontsize=13,
             fontweight='bold', color=C['dark_green'], pad=10)

# Cart
draw_er_table(ax, 0.4, 11.5, 'Cart', [
    ('Id',          'Guid',      'PK'),
    ('CustomerId',  'Guid',      ''),
    ('BudgetLimit', 'decimal?',  ''),
    ('LastUpdated', 'DateTime',  ''),
], width=3.4)

# CartItem
draw_er_table(ax, 0.4, 8.5, 'CartItem', [
    ('Id',        'Guid', 'PK'),
    ('CartId',    'Guid', 'FK'),
    ('ProductId', 'Guid', 'FK'),
    ('Quantity',  'int',  ''),
], width=3.4)

er_arrow(ax, 0.4+3.4/2, 11.5-0.42-4*0.32,
             0.4+3.4/2, 8.5,
         label='1:N')

# Product (projection)
draw_er_table(ax, 0.4, 5.8, 'Product  (local cache)', [
    ('Id',              'Guid',    'PK'),
    ('Name',            'string',  ''),
    ('Price',           'decimal', ''),
    ('DiscountPercent', 'decimal', ''),
    ('ImageUrl',        'string',  ''),
    ('StockQuantity',   'int',     ''),
], width=3.4)

er_arrow(ax, 0.4+3.4/2, 8.5-0.42-2*0.32-0.16,
             0.4+3.4/2, 5.8,
         label='N:1', color=C['teal'])

# Order
draw_er_table(ax, 5.5, 11.5, 'Order', [
    ('Id',                'Guid',      'PK'),
    ('CustomerId',        'Guid',      ''),
    ('CustomerEmail',     'string',    ''),
    ('CustomerFirstName', 'string',    ''),
    ('Status',            'string',    ''),
    ('SubTotal',          'decimal',   ''),
    ('DeliveryFee',       'decimal',   ''),
    ('TaxAmount',         'decimal',   ''),
    ('TotalAmount',       'decimal',   ''),
    ('DiscountAmount',    'decimal',   ''),
    ('DeliveryAddress',   'string',    ''),
    ('Notes',             'string?',   ''),
    ('CreatedAt',         'DateTime',  ''),
    ('EstimatedDelivery', 'DateTime?', ''),
    ('DeliveredAt',       'DateTime?', ''),
], width=3.8)

# OrderItem
draw_er_table(ax, 5.5, 5.8, 'OrderItem', [
    ('Id',          'Guid',    'PK'),
    ('OrderId',     'Guid',    'FK'),
    ('ProductId',   'Guid',    ''),
    ('ProductName', 'string',  ''),
    ('Quantity',    'int',     ''),
    ('UnitPrice',   'decimal', ''),
], width=3.8)

er_arrow(ax, 5.5+3.8/2, 11.5-0.42-15*0.32,
             5.5+3.8/2, 5.8,
         label='1:N')

# Coupon
draw_er_table(ax, 10.8, 11.5, 'Coupon', [
    ('Id',             'Guid',      'PK'),
    ('Code',           'string',    ''),
    ('DiscountType',   'string',    ''),
    ('DiscountValue',  'decimal',   ''),
    ('MinOrderAmount', 'decimal',   ''),
    ('ExpiresAt',      'DateTime?', ''),
    ('IsActive',       'bool',      ''),
    ('UsageLimit',     'int',       ''),
    ('UsedCount',      'int',       ''),
], width=3.5)

ax.text(10.8, 5.5,
        'Status values:\nPending → Processing → Shipped\n→ OutForDelivery → Delivered\n→ Cancelled\n\n'
        'Coupon is standalone — applied\nat checkout, not FK-linked to Order.',
        fontsize=7.5, color=C['dark_green'],
        bbox=dict(boxstyle='round,pad=0.35', facecolor=C['pale_green'],
                  edgecolor=C['border'], linewidth=0.8))

save(fig, '06_er_order.png')


# ══════════════════════════════════════════════════════════════════════════════
# 07 — ER: PaymentService
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 07_er_payment.png ...')
fig, ax = plt.subplots(figsize=(7, 9))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 7); ax.set_ylim(0, 9)
ax.axis('off')
ax.set_title('ER Diagram — PaymentService  (FreshMart_Payment)', fontsize=13,
             fontweight='bold', color=C['dark_green'], pad=10)

draw_er_table(ax, 1.0, 8.5, 'Payment', [
    ('Id',                 'Guid',         'PK'),
    ('UserId',             'Guid',         ''),
    ('OrderId',            'Guid',         ''),
    ('Amount',             'decimal',      ''),
    ('Currency',           'string',       ''),
    ('RazorpayOrderId',    'string',       ''),
    ('RazorpayPaymentId',  'string?',      ''),
    ('RazorpaySignature',  'string?',      ''),
    ('Status',             'PaymentStatus',''),
    ('PaymentMethod',      'string?',      ''),
    ('FailureReason',      'string?',      ''),
    ('CreatedAt',          'DateTime',     ''),
    ('CompletedAt',        'DateTime?',    ''),
    ('Metadata',           'string? (JSON)',''),
], width=5.0)

ax.text(1.0, 1.8,
        'PaymentStatus enum:\n'
        '  0 = Pending    1 = Paid\n'
        '  2 = Failed     3 = Refunded    4 = Cancelled\n\n'
        'UserId / OrderId are cross-service Guid references.\n'
        'No enforced FK constraints to other databases.',
        fontsize=8, color=C['dark_green'],
        bbox=dict(boxstyle='round,pad=0.4', facecolor=C['pale_green'],
                  edgecolor=C['border'], linewidth=1))

save(fig, '07_er_payment.png')

# ══════════════════════════════════════════════════════════════════════════════
# 08 — ER: NotificationService
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 08_er_notification.png ...')
fig, ax = plt.subplots(figsize=(8, 7))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 8); ax.set_ylim(0, 7)
ax.axis('off')
ax.set_title('ER Diagram — NotificationService  (FreshMart_Notification)', fontsize=13,
             fontweight='bold', color=C['dark_green'], pad=10)

draw_er_table(ax, 1.5, 6.5, 'Notification', [
    ('Id',        'Guid',     'PK'),
    ('UserId',    'Guid',     ''),
    ('Title',     'string',   ''),
    ('Message',   'string',   ''),
    ('Type',      'string',   ''),
    ('Link',      'string?',  ''),
    ('IsRead',    'bool',     ''),
    ('CreatedAt', 'DateTime', ''),
], width=5.0)

ax.text(1.5, 2.2,
        'Type values:  info | success | warning | error | order\n\n'
        'UserId is a cross-service Guid reference (from AuthService JWT).\n'
        'SignalR routes push to group  user:{UserId}.\n'
        'Consumers: OrderPlacedEvent → confirmation push + email\n'
        '           OrderStatusChangedEvent → status update push + email',
        fontsize=8, color=C['dark_green'],
        bbox=dict(boxstyle='round,pad=0.4', facecolor=C['pale_green'],
                  edgecolor=C['border'], linewidth=1))

save(fig, '08_er_notification.png')


# ══════════════════════════════════════════════════════════════════════════════
# 09 — ER: SupportService
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 09_er_support.png ...')
fig, ax = plt.subplots(figsize=(13, 10))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 13); ax.set_ylim(0, 10)
ax.axis('off')
ax.set_title('ER Diagram — SupportService  (FreshMart_Support)', fontsize=13,
             fontweight='bold', color=C['dark_green'], pad=10)

# SupportTicket
draw_er_table(ax, 0.5, 9.5, 'SupportTicket', [
    ('Id',            'Guid',      'PK'),
    ('CustomerId',    'Guid',      ''),
    ('CustomerName',  'string',    ''),
    ('CustomerEmail', 'string',    ''),
    ('Subject',       'string',    ''),
    ('Category',      'string',    ''),
    ('Status',        'string',    ''),
    ('Priority',      'string',    ''),
    ('CreatedAt',     'DateTime',  ''),
    ('UpdatedAt',     'DateTime',  ''),
    ('ResolvedAt',    'DateTime?', ''),
], width=4.0)

# SupportMessage
draw_er_table(ax, 6.5, 9.5, 'SupportMessage', [
    ('Id',         'Guid',     'PK'),
    ('TicketId',   'Guid',     'FK'),
    ('SenderId',   'Guid',     ''),
    ('SenderName', 'string',   ''),
    ('SenderRole', 'string',   ''),
    ('Message',    'string',   ''),
    ('IsStaff',    'bool',     ''),
    ('CreatedAt',  'DateTime', ''),
], width=4.0)

er_arrow(ax, 0.5+4.0, 9.5-0.42-5*0.32-0.16,
             6.5,      9.5-0.42-1*0.32-0.16,
         label='1:N')

ax.text(0.5, 2.5,
        'Status:   Open → InProgress → Resolved → Closed\n'
        '          Auto-transitions to InProgress on first staff reply (IsStaff=true)\n\n'
        'Category: Order | Payment | Delivery | Product | Other\n'
        'Priority: Low | Medium | High\n\n'
        'SenderRole: Customer | Admin | StoreManager\n'
        'SignalR broadcasts to group  ticket:{TicketId}',
        fontsize=8, color=C['dark_green'],
        bbox=dict(boxstyle='round,pad=0.4', facecolor=C['pale_green'],
                  edgecolor=C['border'], linewidth=1))

save(fig, '09_er_support.png')


# ══════════════════════════════════════════════════════════════════════════════
# 10 — CHECKOUT FLOW
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 10_checkout_flow.png ...')
fig, ax = plt.subplots(figsize=(8, 18))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 8); ax.set_ylim(0, 18)
ax.axis('off')
ax.set_title('Checkout & Payment Flow', fontsize=14,
             fontweight='bold', color=C['dark_green'], pad=10)

def flow_box(ax, x, y, w, h, text, color, text_color='white', shape='rect', fontsize=8.5):
    if shape == 'diamond':
        pts = np.array([[x, y+h/2],[x+w/2, y+h],[x+w, y+h/2],[x+w/2, y]])
        ax.add_patch(plt.Polygon(pts, closed=True, facecolor=color,
                                 edgecolor=C['border'], linewidth=1.2, zorder=2))
    else:
        ax.add_patch(FancyBboxPatch((x, y), w, h,
                     boxstyle='round,pad=0.04', facecolor=color,
                     edgecolor=C['border'], linewidth=1.2, zorder=2))
    ax.text(x+w/2, y+h/2, text, ha='center', va='center',
            fontsize=fontsize, color=text_color, fontweight='bold',
            zorder=3, multialignment='center')

def flow_arrow(ax, x, y1, y2, label=''):
    ax.annotate('', xy=(x, y2), xytext=(x, y1),
                arrowprops=dict(arrowstyle='->', color=C['arrow'], lw=1.5))
    if label:
        ax.text(x+0.08, (y1+y2)/2, label, fontsize=7, color=C['arrow'])

cx = 4.0; bw = 3.6; bh = 0.55; gap = 0.25

steps = [
    (17.2, 'Customer clicks Checkout',                    C['dark_green']),
    (16.3, 'Validate cart not empty\n& stock available',  C['mid_green'],  'diamond'),
    (15.3, 'Apply coupon (if any)\nValidate & compute discount', C['mid_green']),
    (14.5, 'Calculate totals\n(SubTotal + Tax + DeliveryFee - Discount)', C['mid_green']),
    (13.6, 'POST /api/v1/orders\n→ OrderService creates Order (Pending)', C['dark_green']),
    (12.7, 'OrderService calls\nPOST /api/v1/payment/create-order',       C['blue']),
    (11.8, 'PaymentService creates\nRazorpay order → returns razorpayOrderId', C['blue']),
    (10.9, 'Frontend opens\nRazorpay Checkout Modal',                     C['teal']),
    (10.0, 'Customer completes\npayment on Razorpay',                     C['teal'],  'diamond'),
    (9.1,  'POST /api/v1/payment/verify\n→ HMAC SHA256 signature check',  C['blue']),
    (8.2,  'Payment.Status = Paid\nRazorpayPaymentId saved',              C['blue']),
    (7.3,  'POST /orders/{id}/complete-payment\n→ Cart cleared',          C['dark_green']),
    (6.4,  'Publish OrderPlacedEvent\n→ RabbitMQ',                        C['orange']),
    (5.5,  'ProductService consumer\n→ Decrement stock',                  C['mid_green']),
    (4.6,  'NotificationService consumer\n→ In-app push + Email',         C['purple']),
    (3.7,  'Order status → Processing\n(via PaymentCompletedEvent)',       C['dark_green']),
    (2.8,  'Customer sees\nOrder Confirmation',                           C['light_green']),
]

for i, step in enumerate(steps):
    y = step[0]; text = step[1]; color = step[2]
    shape = step[3] if len(step) > 3 else 'rect'
    h = bh * 1.3 if shape == 'diamond' else bh
    flow_box(ax, cx - bw/2, y - h/2, bw, h, text, color, shape=shape)
    if i < len(steps)-1:
        next_y = steps[i+1][0]
        next_h = bh * 1.3 if (len(steps[i+1]) > 3 and steps[i+1][3] == 'diamond') else bh
        flow_arrow(ax, cx, y - h/2, next_y + next_h/2)

# Fail paths
ax.annotate('', xy=(cx+bw/2+0.8, 16.3), xytext=(cx+bw/2, 16.3),
            arrowprops=dict(arrowstyle='->', color=C['red'], lw=1.3))
ax.text(cx+bw/2+0.85, 16.3, 'Stock\ninsufficient\n→ 400 Error',
        fontsize=7, color=C['red'], va='center')

ax.annotate('', xy=(cx+bw/2+0.8, 10.0), xytext=(cx+bw/2, 10.0),
            arrowprops=dict(arrowstyle='->', color=C['red'], lw=1.3))
ax.text(cx+bw/2+0.85, 10.0, 'Payment\nfailed\n→ Status=Failed',
        fontsize=7, color=C['red'], va='center')

save(fig, '10_checkout_flow.png')


# ══════════════════════════════════════════════════════════════════════════════
# 11 — ORDER STATUS LIFECYCLE
# ══════════════════════════════════════════════════════════════════════════════
print('Generating 11_order_status_flow.png ...')
fig, ax = plt.subplots(figsize=(14, 7))
fig.patch.set_facecolor(C['mint'])
ax.set_facecolor(C['mint'])
ax.set_xlim(0, 14); ax.set_ylim(0, 7)
ax.axis('off')
ax.set_title('Order Status Lifecycle', fontsize=14,
             fontweight='bold', color=C['dark_green'], pad=10)

statuses = [
    (1.2,  3.5, 'Pending',        C['amber'],       'white'),
    (3.4,  3.5, 'Processing',     C['blue'],         'white'),
    (5.6,  3.5, 'Shipped',        C['teal'],         'white'),
    (7.8,  3.5, 'Out for\nDelivery', C['purple'],    'white'),
    (10.0, 3.5, 'Delivered',      C['dark_green'],   'white'),
    (7.8,  1.5, 'Cancelled',      C['red'],          'white'),
]
for sx, sy, label, col, tc in statuses:
    rounded_box(ax, sx, sy, 1.8, 0.85, col, label, fontsize=9.5,
                text_color=tc, bold=True, radius=0.06)

# Main flow arrows
for i in range(len(statuses)-2):
    x1 = statuses[i][0] + 0.9
    x2 = statuses[i+1][0] - 0.9
    y  = 3.5
    ax.annotate('', xy=(x2, y), xytext=(x1, y),
                arrowprops=dict(arrowstyle='->', color=C['mid_green'], lw=2.0))

# Cancel arrow from Pending/Processing
ax.annotate('', xy=(7.8, 1.93), xytext=(1.2, 3.08),
            arrowprops=dict(arrowstyle='->', color=C['red'], lw=1.5,
                            connectionstyle='arc3,rad=0.2'))
ax.text(3.5, 2.3, 'Cancel\n(Admin/Customer)', fontsize=7.5, color=C['red'],
        ha='center', style='italic')

# Who triggers each transition
transitions = [
    (2.3, 4.6, 'PaymentCompletedEvent\n(auto via RabbitMQ)'),
    (4.5, 4.6, 'StoreManager\nor Admin'),
    (6.7, 4.6, 'DeliveryDriver\nor Admin'),
    (8.9, 4.6, 'DeliveryDriver\nor Admin'),
]
for tx, ty, label in transitions:
    ax.text(tx, ty, label, fontsize=7.5, color=C['dark_green'],
            ha='center', va='bottom', style='italic',
            bbox=dict(boxstyle='round,pad=0.2', facecolor=C['pale_green'],
                      edgecolor=C['border'], linewidth=0.6))
    ax.plot([tx, tx], [ty, 3.93], color=C['border'], lw=0.8, ls='--')

# Side effects
ax.text(0.3, 0.8,
        'Every status change → OrderStatusChangedEvent published to RabbitMQ\n'
        '→ NotificationService sends in-app push notification + transactional email to customer',
        fontsize=8.5, color=C['dark_green'],
        bbox=dict(boxstyle='round,pad=0.4', facecolor=C['pale_green'],
                  edgecolor=C['border'], linewidth=1))

save(fig, '11_order_status_flow.png')

print('\nAll diagrams generated successfully in diagrams/ folder!')
