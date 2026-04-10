import { Injectable } from '@angular/core';
import { Order } from '../models';

@Injectable({ providedIn: 'root' })
export class InvoiceService {

  downloadInvoice(order: Order): void {
    const html = this.buildInvoiceHtml(order);
    const blob = new Blob([html], { type: 'text/html' });
    const url = URL.createObjectURL(blob);

    // Open in new window and trigger print-to-PDF
    const win = window.open(url, '_blank');
    if (win) {
      win.onload = () => {
        win.focus();
        win.print();
        setTimeout(() => URL.revokeObjectURL(url), 3000);
      };
    }
  }

  private buildInvoiceHtml(order: Order): string {
    const shortId = order.id.slice(0, 8).toUpperCase();
    const date = new Date(order.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'long', year: 'numeric' });
    const itemRows = order.items.map(i => `
      <tr>
        <td>${i.productName}</td>
        <td style="text-align:center">${i.quantity}</td>
        <td style="text-align:right">₹${i.unitPrice.toFixed(2)}</td>
        <td style="text-align:right">₹${(i.quantity * i.unitPrice).toFixed(2)}</td>
      </tr>`).join('');

    return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<title>Invoice #${shortId} - FreshMart</title>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: Arial, sans-serif; color: #1f2937; background: #fff; padding: 40px; }
  .invoice-box { max-width: 700px; margin: 0 auto; }

  /* Header */
  .header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 36px; padding-bottom: 24px; border-bottom: 2px solid #16a34a; }
  .brand { display: flex; align-items: center; gap: 10px; }
  .brand-icon { font-size: 32px; }
  .brand-name { font-size: 26px; font-weight: 800; color: #16a34a; }
  .brand-tagline { font-size: 11px; color: #6b7280; margin-top: 2px; }
  .invoice-meta { text-align: right; }
  .invoice-title { font-size: 22px; font-weight: 800; color: #111827; }
  .invoice-num { font-size: 14px; color: #6b7280; margin-top: 4px; }
  .invoice-date { font-size: 13px; color: #6b7280; margin-top: 2px; }

  /* Info grid */
  .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 28px; }
  .info-box { background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 8px; padding: 16px; }
  .info-label { font-size: 10px; font-weight: 700; color: #9ca3af; text-transform: uppercase; letter-spacing: .08em; margin-bottom: 6px; }
  .info-value { font-size: 13px; color: #374151; line-height: 1.5; }

  /* Status badge */
  .status-badge { display: inline-block; padding: 3px 12px; border-radius: 20px; font-size: 12px; font-weight: 700; background: #dcfce7; color: #15803d; border: 1px solid #bbf7d0; }

  /* Items table */
  table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
  thead tr { background: #16a34a; color: #fff; }
  thead th { padding: 11px 14px; text-align: left; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: .05em; }
  thead th:not(:first-child) { text-align: right; }
  tbody tr { border-bottom: 1px solid #f3f4f6; }
  tbody tr:nth-child(even) { background: #f9fafb; }
  tbody td { padding: 11px 14px; font-size: 13px; color: #374151; }

  /* Totals */
  .totals { margin-left: auto; width: 280px; }
  .total-row { display: flex; justify-content: space-between; padding: 6px 0; font-size: 13px; color: #6b7280; border-bottom: 1px solid #f3f4f6; }
  .total-row.discount { color: #16a34a; font-weight: 600; }
  .total-row.grand { font-size: 16px; font-weight: 800; color: #111827; border-top: 2px solid #16a34a; border-bottom: none; padding-top: 10px; margin-top: 4px; }

  /* Footer */
  .footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #e5e7eb; text-align: center; }
  .footer p { font-size: 12px; color: #9ca3af; margin-bottom: 4px; }
  .footer .thank-you { font-size: 15px; font-weight: 700; color: #16a34a; margin-bottom: 8px; }

  @media print {
    body { padding: 20px; }
    @page { margin: 1cm; }
  }
</style>
</head>
<body>
<div class="invoice-box">

  <div class="header">
    <div class="brand">
      <span class="brand-icon">🛒</span>
      <div>
        <div class="brand-name">FreshMart</div>
        <div class="brand-tagline">Fresh groceries delivered to your door</div>
      </div>
    </div>
    <div class="invoice-meta">
      <div class="invoice-title">INVOICE</div>
      <div class="invoice-num">#${shortId}</div>
      <div class="invoice-date">${date}</div>
    </div>
  </div>

  <div class="info-grid">
    <div class="info-box">
      <div class="info-label">Order Details</div>
      <div class="info-value">
        <strong>Order ID:</strong> #${shortId}<br/>
        <strong>Date:</strong> ${date}<br/>
        <strong>Status:</strong> <span class="status-badge">${order.status}</span>
      </div>
    </div>
    <div class="info-box">
      <div class="info-label">Delivery Address</div>
      <div class="info-value">${order.deliveryAddress || 'N/A'}</div>
    </div>
  </div>

  <table>
    <thead>
      <tr>
        <th>Item</th>
        <th style="text-align:right">Qty</th>
        <th style="text-align:right">Unit Price</th>
        <th style="text-align:right">Total</th>
      </tr>
    </thead>
    <tbody>
      ${itemRows}
    </tbody>
  </table>

  <div class="totals">
    <div class="total-row"><span>Subtotal</span><span>₹${order.subTotal.toFixed(2)}</span></div>
    <div class="total-row"><span>Delivery Fee</span><span>${order.deliveryFee === 0 ? 'FREE' : '₹' + order.deliveryFee.toFixed(2)}</span></div>
    <div class="total-row"><span>Tax (5%)</span><span>₹${order.taxAmount.toFixed(2)}</span></div>
    ${order.discountAmount > 0 ? `<div class="total-row discount"><span>Discount</span><span>- ₹${order.discountAmount.toFixed(2)}</span></div>` : ''}
    <div class="total-row grand"><span>Grand Total</span><span>₹${order.totalAmount.toFixed(2)}</span></div>
  </div>

  <div class="footer">
    <p class="thank-you">Thank you for shopping with FreshMart! 🎉</p>
    <p>For support, contact us at support@freshmart.com</p>
    <p>This is a computer-generated invoice and does not require a signature.</p>
  </div>

</div>
</body>
</html>`;
  }
}
