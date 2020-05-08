import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { ImageService } from 'src/app/services/image.service';

@Component({
	templateUrl: './races.component.html',
	styleUrls: ['./races.component.scss'],
})
export class RacesComponent implements OnInit {
	constructor(
		private fb: FormBuilder,
		private route: ActivatedRoute,
		private router: Router,
		private imageService: ImageService
	) {}

	form = this.fb.group({
		name: ['', Validators.required],
		description: ['', Validators.required],
		imagekey: [''],
	});

	dialog = false;
	mudId: string;
	selectedFile: File = null;

	//Todo Interface muss implementiert werden
	races: Array<{ name: string; description: string; imagekey: string }> = [];

	ngOnInit(): void {
		/* Daten fetchen und in Array laden */

		this.mudId = this.route.snapshot.params.mudid;
	}

	changeDialog() {
		this.form.reset();
		this.dialog = !this.dialog;
	}

	onAbort() {
		this.router.navigate(['/my-muds']);
	}

	async addRace() {
		let imageKey = null;
		if (this.selectedFile != null) {
			imageKey = await this.imageService.uploadFile(this.selectedFile);
		}

		this.races.push({
			name: this.form.get('name').value,
			description: this.form.get('description').value,
			imagekey: imageKey,
		});

		this.selectedFile = null;

		this.changeDialog();
	}

	onFileSelected(event) {
		this.selectedFile = <File>event.target.files[0];
	}

	deleteRow(index: number) {
		this.races.splice(index, 1);
	}

	async onSubmit() {
		/* Object erstellen */
		/* Request zur API schicken */

		//Redirect zur nächsten Konfigurationsseite
		this.router.navigate(['/my-muds/' + this.mudId + '/classes']);
	}
}
