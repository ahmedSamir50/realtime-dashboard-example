import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';

export interface StockPriceUpdate {
  ticker: string;
  price: number;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection: signalR.HubConnection | undefined;
  public stockPrice$ = new Subject<StockPriceUpdate>();

  constructor() { }

  public startConnection = (hubUrl: string): Promise<void> => {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    this.registerSignalREvents();

    return this.hubConnection
      .start()
      .then(() => console.info('SignalR: Connection started'))
      .catch(err => {
        console.error('SignalR: Error while starting connection: ' + err);
        throw err;
      });
  }

  public joinStockGroupInterest = (stockTicker: string) => {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      console.info(`SignalR: Joining group for ticker ${stockTicker}`);
      this.hubConnection.invoke('JoinStockGroupInterest', stockTicker)
        .catch(err => console.error(`SignalR: Error joining group ${stockTicker}: ` + err));
    } else {
      console.warn(`SignalR: Cannot join group ${stockTicker}, connection state is: ${this.hubConnection?.state}`);
    }
  }

  private registerSignalREvents() {
    this.hubConnection?.on('ReciveStockPriceUpdate', (data: any) => {
      console.info('SignalR: Received stock update: ', data);
      // Map stockSymbolTicker to ticker if it exists
      const update: StockPriceUpdate = {
        ticker: data.stockSymbolTicker || data.ticker,
        price: data.price
      };

      this.stockPrice$.next(update);
    });
  }
}
