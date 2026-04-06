import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  IonHeader, IonToolbar, IonTitle, IonContent,
  IonGrid, IonRow, IonCol, IonCard, IonCardHeader,
  IonCardTitle, IonCardContent, IonIcon, IonBadge,
  IonSearchbar, IonButton, IonFab, IonFabButton,
  IonList, IonItem, IonLabel
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { trendingUpOutline, trendingDownOutline, statsChartOutline, addOutline, closeOutline, notificationsOutline } from 'ionicons/icons';
import { SignalrService, StockPriceUpdate } from '../services/signalr.service';
import { StockApiService } from '../services/stock-api.service';
import { Subscription } from 'rxjs';
import { environment } from '../../environments/environment';
import { Router } from '@angular/router';
import { HistoryPage } from '../history/history.page';

interface StockDisplay extends StockPriceUpdate {
  previousPrice: number;
  changeAmount: number;
  changePercentage: number;
  changeStatus: 'up' | 'down' | 'neutral';
}

@Component({
  selector: 'app-home',
  templateUrl: 'home.page.html',
  styleUrls: ['home.page.scss'],
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    IonHeader, IonToolbar, IonTitle, IonContent,
    IonGrid, IonRow, IonCol, IonCard,
    IonCardHeader, IonCardTitle,
    IonCardContent, IonIcon, IonBadge,
    IonSearchbar, IonButton, IonFab, IonFabButton,
    IonList, IonItem, IonLabel
  ],
})
export class HomePage implements OnInit, OnDestroy {
  public stocks: StockDisplay[] = [];

  private subscription: Subscription | undefined;
  public newTicker: string = '';
  public updatedTickers: Set<string> = new Set();

  public searchResults: any[] = [];

  constructor(
    private signalrService: SignalrService,
    private stockApiService: StockApiService,
    private router: Router
  ) {
    addIcons({ trendingUpOutline, trendingDownOutline, statsChartOutline, addOutline, closeOutline, notificationsOutline });
  }

  async ngOnInit() {
    this.loadStocks();
    const hubUrl = `${environment.apiBaseUrl}/stocks-feed`;
    try {
      await this.signalrService.startConnection(hubUrl);
      // Join groups for all loaded stocks
      this.stocks.forEach(s => this.signalrService.joinStockGroupInterest(s.ticker));
      // Fetch initial prices for all
      this.refreshAllPrices();
    } catch (err) {
      console.error('Failed to start SignalR connection:', err);
    }

    this.subscription = this.signalrService.stockPrice$.subscribe(update => this.updateStockPrice(update));
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  private saveStocks() {
    const tickers = this.stocks.map(s => s.ticker);
    localStorage.setItem('user_stocks', JSON.stringify(tickers));
  }

  private loadStocks() {
    const saved = localStorage.getItem('user_stocks');
    if (saved) {
      const tickers: string[] = JSON.parse(saved);
      this.stocks = tickers.map(ticker => ({
        ticker, price: 0, previousPrice: 0, changeAmount: 0, changePercentage: 0, changeStatus: 'neutral'
      }));
    }
  }

  private async refreshAllPrices() {
    for (const stock of this.stocks) {
      this.fetchInitialPrice(stock);
    }
  }

  private async fetchInitialPrice(stock: StockDisplay) {
    this.stockApiService.getStockPrice(stock.ticker).subscribe(data => {
      if (data) {
        stock.price = data.price;
        stock.previousPrice = data.price;
        stock.changeStatus = 'neutral';
      }
    });
  }

  public async onSearchChange(event: any) {
    const query = event.detail.value;
    if (query && query.length > 1) {
      this.stockApiService.searchStocks(query).subscribe(results => {
        this.searchResults = results;
      });
    } else {
      this.searchResults = [];
    }
  }

  public selectSuggestion(suggestion: any) {
    this.newTicker = suggestion.ticker;
    this.searchResults = [];
    this.addStock();
  }

  public async addStock() {
    const ticker = this.newTicker.trim().toUpperCase();
    if (ticker && !this.stocks.some(s => s.ticker === ticker)) {
      const newStock: StockDisplay = {
        ticker, price: 0, previousPrice: 0, changeAmount: 0, changePercentage: 0, changeStatus: 'neutral'
      };
      this.stocks.push(newStock);
      this.saveStocks();
      this.signalrService.joinStockGroupInterest(ticker);
      this.fetchInitialPrice(newStock);
      this.newTicker = '';
      this.searchResults = [];
    }
  }

  public removeStock(ticker: string) {
    this.stocks = this.stocks.filter(s => s.ticker !== ticker);
    this.saveStocks();
  }

  public onCardClick(stock: StockDisplay) {
    this.router.navigate(['/history', stock.ticker]);
  }

  private updateStockPrice(update: StockPriceUpdate) {
    const stock = this.stocks.find(s => s.ticker === update.ticker);
    if (stock) {
      if (update.price === stock.price) return;

      stock.changeStatus = update.price > stock.price ? 'up' : 'down';
      stock.previousPrice = stock.price || update.price;
      stock.price = update.price;
      stock.changeAmount = stock.price - stock.previousPrice;
      stock.changePercentage = (stock.changeAmount / stock.previousPrice) * 100;

      // Pulse animation trigger
      this.updatedTickers.add(update.ticker);
      setTimeout(() => this.updatedTickers.delete(update.ticker), 400);
    }
  }
}
