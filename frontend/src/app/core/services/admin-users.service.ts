import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { UserAdmin, UserCreateRequest, UserCredentials } from "../models/user.model";

@Injectable({ providedIn: "root" })
export class AdminUsersService {
    private base = `${environment.apiUrl}/admin/users`;

    constructor(private http: HttpClient) {}

    list(): Observable<UserAdmin[]> {
        return this.http.get<UserAdmin[]>(this.base);
    }

    create(data: UserCreateRequest): Observable<UserCredentials> {
        return this.http.post<UserCredentials>(this.base, data);
    }

    resetPassword(id: number, newPassword?: string): Observable<UserCredentials> {
        return this.http.post<UserCredentials>(`${this.base}/${id}/reset-password`, {
            newPassword: newPassword || null,
        });
    }

    activate(id: number): Observable<void> {
        return this.http.put<void>(`${this.base}/${id}/activate`, {});
    }

    deactivate(id: number): Observable<void> {
        return this.http.post<void>(`${this.base}/${id}/deactivate`, {});
    }
}
