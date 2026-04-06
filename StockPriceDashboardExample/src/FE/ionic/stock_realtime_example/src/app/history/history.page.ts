import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import {
  IonHeader, IonToolbar, IonTitle, IonContent,
  IonButtons, IonBackButton, IonIcon, IonSpinner,
  IonList, IonItem, IonLabel, IonNote, IonSegment, IonSegmentButton,
  IonInfiniteScroll, IonInfiniteScrollContent
} from '@ionic/angular/standalone';
import { statsChartOutline, calendarOutline } from 'ionicons/icons';
import { addIcons } from 'ionicons';
import { StockApiService } from '../services/stock-api.service';
import { tap, catchError, finalize } from 'rxjs/operators';
import { of } from 'rxjs';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [
    CommonModule,
    IonHeader, IonToolbar, IonTitle, IonContent,
    IonButtons, IonBackButton, IonIcon, IonSpinner,
    IonList, IonItem, IonLabel, IonNote, IonSegment, IonSegmentButton,
    IonInfiniteScroll, IonInfiniteScrollContent
  ],
  template: `
    <ion-header class="ion-no-border">
      <ion-toolbar class="history-toolbar">
        <ion-buttons slot="start">
          <ion-back-button defaultHref="/home"></ion-back-button>
        </ion-buttons>
        <ion-title>{{ ticker }} Pulse Insights</ion-title>
        <ion-icon name="stats-chart-outline" slot="end" class="header-icon"></ion-icon>
      </ion-toolbar>
    </ion-header>

    <ion-content class="history-content">
      <div class="glass-container">
        <!-- Loading Overlay for Period Updates -->
        <div *ngIf="loading" class="update-loader">
          <ion-spinner name="crescent" color="primary"></ion-spinner>
          <span>Refreshing pulse...</span>
        </div>

        <!-- Period Selector -->
        <ion-segment [value]="selectedPeriod" (ionChange)="onPeriodChange($event)" class="period-segment" [disabled]="loading">
          <ion-segment-button value="7"><ion-label>1W</ion-label></ion-segment-button>
          <ion-segment-button value="30"><ion-label>1M</ion-label></ion-segment-button>
          <ion-segment-button value="90"><ion-label>3M</ion-label></ion-segment-button>
          <ion-segment-button value="365"><ion-label>1Y</ion-label></ion-segment-button>
          <ion-segment-button value="1825"><ion-label>5Y</ion-label></ion-segment-button>
        </ion-segment>

        <div class="history-results" [class.content-faded]="loading" *ngIf="history.length > 0">
          <!-- Professional Chart.js Canvas -->
          <div class="chart-container">
            <canvas #historyChart></canvas>
          </div>

          <!-- Paginated History List -->
          <ion-list lines="none" class="history-list">
            <ion-item *ngFor="let entry of displayedHistory" class="history-item animated-item">
              <div class="date-group" slot="start">
                <ion-icon name="calendar-outline"></ion-icon>
                <div class="date-text">
                  <h2>{{ entry.date | date:'EEE, MMM d, y' }}</h2>
                  <p>{{ entry.date | date:'shortTime' }}</p>
                </div>
              </div>
              <ion-note slot="end" class="price-note">
                {{ entry.price | currency:'USD' }}
              </ion-note>
            </ion-item>
          </ion-list>

          <ion-infinite-scroll (ionInfinite)="loadMore($event)" threshold="150px" [disabled]="isAllLoaded || loading">
            <ion-infinite-scroll-content loadingSpinner="crescent" loadingText="Fetching historical archive...">
            </ion-infinite-scroll-content>
          </ion-infinite-scroll>
        </div>

        <div *ngIf="!loading && history.length === 0" class="empty-state">
             <ion-icon name="stats-chart-outline"></ion-icon>
             <p>No historical data available for this cycle.</p>
        </div>
      </div>
    </ion-content>
  `,
  styles: [`
    .history-toolbar {
      --background: rgba(13, 17, 23, 0.95);
      --color: white;
    }
    .header-icon {
        margin-right: 16px; font-size: 1.4rem; color: #00d2ff;
    }
    .history-content {
      --background: #0d1117; color: white;
    }
    .period-segment {
        margin-bottom: 24px;
        --background: rgba(255, 255, 255, 0.03);
        border: 1px solid rgba(255, 255, 255, 0.05);
        border-radius: 12px;
    }
    .glass-container {
      padding: 20px; padding-top: 10px;
      position: relative;
    }
    .update-loader {
      position: absolute;
      top: 50%; left: 50%;
      transform: translate(-50%, -50%);
      z-index: 100;
      display: flex; flex-direction: column; align-items: center; gap: 10px;
      background: rgba(13, 17, 23, 0.8);
      padding: 20px; border-radius: 16px;
      backdrop-filter: blur(8px);
      border: 1px solid rgba(255,255,255,0.1);
      color: #00d2ff; font-weight: 600;
    }
    .content-faded {
      opacity: 0.3;
      pointer-events: none;
      filter: blur(2px);
      transition: all 0.3s ease;
    }
    .chart-container {
        background: rgba(255, 255, 255, 0.02);
        border-radius: 20px;
        padding: 15px;
        margin-bottom: 30px;
        border: 1px solid rgba(255, 255, 255, 0.05);
        height: 250px;
        position: relative;
    }
    .date-group {
        display: flex; align-items: center; gap: 12px;
        color: rgba(255, 255, 255, 0.7);
    }
    .date-text h2 { margin: 0; font-size: 0.95rem; color: white; }
    .date-text p { margin: 0; font-size: 0.8rem; }
    .loading-state, .empty-state {
        display: flex; flex-direction: column; align-items: center; justify-content: center;
        height: 50vh; color: rgba(255, 255, 255, 0.5);
    }
    .history-list { background: transparent; }
    .history-item {
        --background: rgba(255, 255, 255, 0.02);
        --border-radius: 16px;
        margin-bottom: 10px;
        border: 1px solid rgba(255, 255, 255, 0.05);
        --padding-start: 16px;
    }
    .price-note {
        font-weight: 700; color: #00d2ff; font-size: 1.1rem;
    }
  `]
})
export class HistoryPage implements OnInit, OnDestroy {
  @ViewChild('historyChart') chartCanvas!: ElementRef;

  public ticker: string = '';
  public history: any[] = [];
  public displayedHistory: any[] = [];
  public loading = true;
  public selectedPeriod = '7';

  private chart: Chart | undefined;

  // Pagination logic
  private pageSize = 20;
  private currentPage = 1;
  public isAllLoaded = false;

  constructor(
    private route: ActivatedRoute,
    private stockApiService: StockApiService
  ) {
    addIcons({ statsChartOutline, calendarOutline });
  }

  ngOnInit() {
    this.ticker = this.route.snapshot.paramMap.get('ticker') || '';
    this.loadHistory();
  }

  ngOnDestroy() {
    if (this.chart) {
      this.chart.destroy();
    }
  }

  onPeriodChange(event: any) {
    this.selectedPeriod = event.detail.value;
    this.loadHistory();
  }

  loadHistory() {
    this.loading = true;
    this.isAllLoaded = false;
    this.currentPage = 1;

    this.stockApiService.getStockHistory(this.ticker, parseInt(this.selectedPeriod)).pipe(
      tap(data => {
        // Chronological order for Chart.js
        this.history = [...data].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
        this.updateChart();
        this.updateDisplayedHistory();
      }),
      catchError(err => {
        console.error('History API Error:', err);
        return of([]);
      }),
      finalize(() => this.loading = false)
    ).subscribe();
  }

  updateDisplayedHistory() {
    const sortedList = [...this.history].reverse();
    const end = this.currentPage * this.pageSize;
    this.displayedHistory = sortedList.slice(0, end);
    this.isAllLoaded = this.displayedHistory.length >= this.history.length;
  }

  loadMore(event: any) {
    setTimeout(() => {
      this.currentPage++;
      this.updateDisplayedHistory();
      event.target.complete();
    }, 300);
  }

  updateChart() {
    if (!this.chartCanvas) return;

    const ctx = this.chartCanvas.nativeElement.getContext('2d');
    const labels = this.history.map(h => new Date(h.date).toLocaleDateString());
    const prices = this.history.map(h => h.price);

    const isUp = prices[prices.length - 1] >= prices[0];
    const accentColor = isUp ? '#00e676' : '#ff3d00';

    if (this.chart) {
      this.chart.data.labels = labels;
      this.chart.data.datasets[0].data = prices;
      this.chart.data.datasets[0].borderColor = accentColor;
      this.chart.data.datasets[0].backgroundColor = this.getGradient(ctx, accentColor);
      this.chart.update();
      return;
    }

    this.chart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: labels,
        datasets: [{
          label: 'Market Price',
          data: prices,
          borderColor: accentColor,
          backgroundColor: this.getGradient(ctx, accentColor),
          borderWidth: 3,
          pointRadius: 0,
          pointHoverRadius: 6,
          fill: true,
          tension: 0.4
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            mode: 'index',
            intersect: false,
            backgroundColor: 'rgba(13, 17, 23, 0.9)',
            titleColor: 'rgba(255,255,255,0.6)',
            bodyColor: 'white',
            bodyFont: { weight: 'bold' },
            padding: 12,
            displayColors: false,
            callbacks: {
              label: (context) => `$${context!.parsed!.y!.toLocaleString(undefined, { minimumFractionDigits: 2 })}`
            }
          }
        },
        scales: {
          x: { display: false },
          y: {
            display: true,
            grid: { color: 'rgba(255,255,255,0.05)' },
            ticks: { color: 'rgba(255,255,255,0.4)', font: { size: 10 } }
          }
        }
      }
    });
  }

  private getGradient(ctx: CanvasRenderingContext2D, color: string) {
    const gradient = ctx.createLinearGradient(0, 0, 0, 250);
    gradient.addColorStop(0, color + '4D'); // 30% opacity
    gradient.addColorStop(1, color + '00'); // transparent
    return gradient;
  }
}
