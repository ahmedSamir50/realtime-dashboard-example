import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  IonHeader, IonToolbar, IonTitle, IonContent,
  IonButtons, IonButton, IonIcon, IonSpinner,
  IonList, IonItem, IonLabel, IonNote
} from '@ionic/angular/standalone';
import { closeOutline, statsChartOutline } from 'ionicons/icons';
import { addIcons } from 'ionicons';
import { ModalController } from '@ionic/angular/standalone';
import { StockApiService } from '../services/stock-api.service';
import { tap, catchError, finalize } from 'rxjs/operators';
import { of } from 'rxjs';

@Component({
  selector: 'app-history-modal',
  standalone: true,
  imports: [
    CommonModule,
    IonHeader, IonToolbar, IonTitle, IonContent,
    IonButtons, IonButton, IonIcon, IonSpinner,
    IonList, IonItem, IonLabel, IonNote
  ],
  template: `
    <ion-header class="ion-no-border">
      <ion-toolbar class="modal-toolbar">
        <ion-title>{{ ticker }} Pulse History</ion-title>
        <ion-buttons slot="end">
          <ion-button (click)="dismiss()">
            <ion-icon name="close-outline"></ion-icon>
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="modal-content">
      <div class="glass-container">
        <div *ngIf="loading" class="loading-state">
          <ion-spinner name="crescent" color="primary"></ion-spinner>
          <p>Analyzing Market Trends...</p>
        </div>

        <div *ngIf="!loading && history.length > 0" class="history-results">
          <!-- Premium SVG Sparkline -->
          <div class="chart-wrapper">
            <svg viewBox="0 0 400 150" class="sparkline-svg">
              <defs>
                <linearGradient id="chartGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                  <stop offset="0%" [attr.stop-color]="getTrendColor()" stop-opacity="0.3" />
                  <stop offset="100%" [attr.stop-color]="getTrendColor()" stop-opacity="0" />
                </linearGradient>
              </defs>
              <path [attr.d]="svgPath" fill="none" [attr.stroke]="getTrendColor()" stroke-width="3" stroke-linecap="round" />
              <path [attr.d]="fillPath" fill="url(#chartGradient)" />
            </svg>
          </div>

          <ion-list lines="none" class="history-list">
            <ion-item *ngFor="let entry of history" class="history-item">
              <ion-label>
                <h2>{{ entry.date | date:'EEE, MMM d' }}</h2>
                <p>{{ entry.date | date:'shortTime' }}</p>
              </ion-label>
              <ion-note slot="end" class="price-note">
                {{ entry.price | currency:'USD' }}
              </ion-note>
            </ion-item>
          </ion-list>
        </div>

        <div *ngIf="!loading && history.length === 0" class="empty-state">
             <ion-icon name="stats-chart-outline"></ion-icon>
             <p>No historical data available for this cycle.</p>
        </div>
      </div>
    </ion-content>
  `,
  styles: [`
    .modal-toolbar {
      --background: rgba(13, 17, 23, 0.95);
      --color: white;
      text-align: center;
    }
    .modal-content {
      --background: #0d1117;
      --color: white;
    }
    .glass-container {
      padding: 20px;
      height: 100%;
    }
    .chart-wrapper {
        background: rgba(255, 255, 255, 0.03);
        border-radius: 16px;
        padding: 20px;
        margin-bottom: 24px;
        border: 1px solid rgba(255, 255, 255, 0.05);
    }
    .sparkline-svg {
        width: 100%;
        height: auto;
        overflow: visible;
        filter: drop-shadow(0 0 8px rgba(0, 163, 255, 0.2));
    }
    .loading-state, .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 60vh;
        color: rgba(255, 255, 255, 0.5);
    }
    .history-list {
        background: transparent;
    }
    .history-item {
        --background: rgba(255, 255, 255, 0.02);
        --border-radius: 12px;
        margin-bottom: 8px;
        border: 1px solid rgba(255, 255, 255, 0.05);
    }
    .price-note {
        font-weight: 600;
        color: #00d2ff;
        font-size: 1rem;
    }
  `]
})
export class HistoryModalComponent implements OnInit {
  @Input() ticker!: string;
  public history: any[] = [];
  public loading = true;
  public svgPath = '';
  public fillPath = '';

  constructor(
    private modalCtrl: ModalController,
    private stockApiService: StockApiService
  ) {
    addIcons({ closeOutline, statsChartOutline });
  }

  ngOnInit() {
    this.loadHistory();
  }

  loadHistory() {
    this.loading = true;
    this.stockApiService.getStockHistory(this.ticker, 14).pipe(
      tap(data => {
        this.history = data;
        this.generateSvgPath();
      }),
      catchError(err => {
        console.error('History Error:', err);
        return of([]);
      }),
      finalize(() => this.loading = false)
    ).subscribe();
  }

  generateSvgPath() {
    if (!this.history || this.history.length === 0) return;

    const prices = this.history.map(h => h.price);
    const min = Math.min(...prices);
    const max = Math.max(...prices);
    const range = max - min || 1;

    const width = 400;
    const height = 150;
    const step = width / (this.history.length - 1);

    const points = this.history.map((h, i) => {
      const x = i * step;
      const y = height - ((h.price - min) / range * (height - 40) + 20);
      return `${x},${y}`;
    });

    this.svgPath = `M ${points.join(' L ')}`;
    this.fillPath = `${this.svgPath} V ${height} H 0 Z`;
  }

  getTrendColor() {
    if (!this.history || this.history.length < 2) return '#00a3ff';
    const latest = this.history[this.history.length - 1].price;
    const first = this.history[0].price;
    return latest >= first ? '#00e676' : '#ff3d00';
  }

  dismiss() {
    this.modalCtrl.dismiss();
  }
}
