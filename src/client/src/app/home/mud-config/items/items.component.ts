import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { IMudItem } from 'src/app/model/muds/MudSetupDTO';
import { MudService } from 'src/app/services/mud.service';
import { IMudItemRequest, IMudItemResponse } from 'src/app/model/muds/MudDTO';
import { IImageUploadResponse } from 'src/app/model/FileUploadDTO';

@Component({
	selector: 'mh-items',
	templateUrl: './items.component.html',
	styleUrls: ['./items.component.scss'],
})
export class ItemsComponent implements OnInit {
	constructor(
		private mudService: MudService,
		private fb: FormBuilder,
		private route: ActivatedRoute,
		private router: Router
	) {}

	form = this.fb.group({
		name: ['', Validators.required],
		description: ['', Validators.required],
		weight: ['', Validators.required],
	});
	mudId: string;
	dialog = false;
	edit = false;
	index: number;
	items: Array<IMudItem> = [];

	async deleteRow(index: number) {
		await this.mudService.deleteItem(this.mudId, this.items[index].itemId);
		this.items.splice(index, 1);
	}

	editRow(index: number) {
		this.edit = true;
		this.dialog = true;
		this.index = index;
		this.form.get('name').setValue(this.items[index].name);
		this.form.get('description').setValue(this.items[index].description);
		this.form.get('weight').setValue(this.items[index].weight);
	}

	async ngOnInit() {
		this.mudId = this.route.snapshot.params.mudid;
		this.items = await this.mudService.getItemsForMud(this.mudId);
	}

	changeDialog() {
		this.form.reset();
		this.dialog = !this.dialog;
		this.edit = false;
	}

	async addItem() {
		const imageKey: IImageUploadResponse = null;

		if (!this.edit) {
			const response: IMudItemResponse = await this.mudService.addItem(
				this.mudId,
				{
					name: this.form.get('name').value,
					description: this.form.get('description').value,
					weight: this.form.get('weight').value,
					imageKey: imageKey?.imageUrl,
					mudId: this.mudId,
				}
			);

			if (response.succeeded) {
				this.items.push({
					itemId: response.item.itemId,
					name: response.item.name,
					description: response.item.description,
					weight: response.item.weight,
					imageKey: response.item.imageKey,
				});
			}
		} else {
			const response: IMudItemResponse = await this.mudService.editItem(
				this.items[this.index].itemId,
				{
					name: this.form.get('name').value,
					description: this.form.get('description').value,
					weight: this.form.get('weight').value,
					imageKey: imageKey?.imageUrl,
					mudId: this.mudId,
				}
			);

			if (response.succeeded) {
				this.items[this.index] = {
					itemId: response.item.itemId,
					name: response.item.name,
					description: response.item.description,
					weight: response.item.weight,
					imageKey: response.item.imageKey,
				};
			}
		}

		this.changeDialog();
	}
}
