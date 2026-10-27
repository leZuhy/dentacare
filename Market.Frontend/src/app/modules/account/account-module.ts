import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared-module';
import { MyProfileComponent } from './my-profile/my-profile.component';

/**
 * Stranice naloga koje imaju i admin i kupac: "Moj profil" (/admin/profile i /client/profile).
 *
 * Zašto mali modul, a ne SharedModule: SharedModule importuje i AppModule, pa sve u njemu ide u
 * početni bundle – zato su tamo samo sitni dijelovi (pipe, polje greške, header). Stranica profila
 * treba samo admin i client dijelu: AdminModule i ClientModule importuju ovaj modul, a build ga
 * izdvoji u zajednički lazy chunk koji se učita tek kad zatreba.
 *
 * Modul nema svoje rute: svaki dio zadaje rutu u svom routing modulu.
 */
@NgModule({
  declarations: [MyProfileComponent],
  imports: [SharedModule],
  exports: [MyProfileComponent]
})
export class AccountModule {}
