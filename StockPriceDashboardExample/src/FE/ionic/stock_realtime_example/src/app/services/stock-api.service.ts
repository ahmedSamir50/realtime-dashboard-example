import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StockApiService {
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly paths = environment.apiPaths;

  constructor(private http: HttpClient) { }

  /**
   * Fetches the latest price for a single stock ticker
   */
  public getStockPrice(ticker: string): Observable<any> {
    const url = `${this.baseUrl}/${this.paths.stocks}/${ticker}`;
    return this.http.get<any>(url).pipe(
      tap(data => console.info(`StockApiService: Fetched price for ${ticker}`, data)),
      catchError(error => {
        console.error(`StockApiService: Error fetching price for ${ticker}`, error);
        return of(null);
      })
    );
  }

  /**
   * Searches for stocks matching the query
   */
  public searchStocks(query: string): Observable<any[]> {
    const url = `${this.baseUrl}/${this.paths.search}?query=${query}`;
    return this.http.get<any[]>(url).pipe(
      tap(results => console.info(`StockApiService: Search results for "${query}"`, results.length)),
      catchError(error => {
        console.error(`StockApiService: Search error for "${query}"`, error);
        return of([]);
      })
    );
  }

  /**
   * Fetches historical data for a stock
   */
  public getStockHistory(ticker: string, days: number = 7): Observable<any[]> {
    const url = `${this.baseUrl}/${this.paths.stocks}/${ticker}/history?days=${days}`;
    return this.http.get<any[]>(url).pipe(
      tap(history => console.info(`StockApiService: Fetched history for ${ticker}`, history.length)),
      catchError(error => {
        console.error(`StockApiService: History error for ${ticker}`, error);
        return of([]);
      })
    );
  }
}
