import {Component, OnInit} from '@angular/core'
import {Router} from '@angular/router'
import {SupabaseService} from '../services/supabase.service'
import {
  IonButton,
  IonContent,
  IonHeader,
  IonInput,
  IonItem,
  IonLabel,
  IonList,
  IonTitle,
  IonToolbar
} from "@ionic/angular/standalone";
import {FormsModule} from "@angular/forms";
import {TranslateModule} from "@ngx-translate/core";

@Component({
  selector: 'app-account',
  templateUrl: './account.page.html',
  standalone: true,
  imports: [IonButton, IonLabel, IonInput, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonItem, FormsModule, TranslateModule],
})
export class AccountPage implements OnInit {
  email = ''

  constructor(
    private readonly supabase: SupabaseService,
    private router: Router
  ) {
  }

  ngOnInit() {
    this.getEmail()
  }

  async getEmail() {
    this.email = await this.supabase.user.then((user) => user?.email || '')
  }


  async signOut() {
    await this.supabase.signOut()
    return this.router.navigate(['/login'], {replaceUrl: true})
  }
}
