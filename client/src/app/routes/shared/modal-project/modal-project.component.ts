import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { catchError, of } from 'rxjs';
import { Permission } from 'src/app/helpers/PermisionEnum';
import { StatusCode } from 'src/app/helpers/StatusCodeEnum';
import { User } from 'src/app/models/user';
import { AuthenticationService } from 'src/app/services/authentication.service';
import { ProjectService } from 'src/app/services/project.service';
import { UserService } from 'src/app/services/user.service';
import { Priority } from '../priority-icon/priority-icon.component';
@Component({
  selector: 'app-modal-project',
  templateUrl: './modal-project.component.html',
  styleUrls: ['./modal-project.component.css'],
})
export class ModalProjectComponent implements OnInit {
  @Input() departments: any[] = [];
  @Output() onChangeProject = new EventEmitter();
  mode: string = 'create';
  title: string = 'New Project';
  users: any[] = [];
  data: any;
  isEdit: boolean = false;
  user: User;
  constructor(
    private fb: FormBuilder,
    private projectService: ProjectService,
    private userService: UserService,
    private authenticationService: AuthenticationService,
    private toastr: ToastrService
  ) {}
  modalForm!: FormGroup;
  projectTypes: any[] = [
    { value: 'MRP', viewValue: 'Manufacturing Projects' },
    { value: 'CTP', viewValue: 'Construction Projects' },
    { value: 'MNP', viewValue: 'Management Projects' },
    { value: 'RSP', viewValue: 'Research Projects' },
  ];
  statusCode: any[] = [
    { value: StatusCode.Reopened, viewValue: 'Reopened' },
    { value: StatusCode.Resolved, viewValue: 'Resolved' },
    { value: StatusCode.Open, viewValue: 'Open' },
    { value: StatusCode.InProgress, viewValue: 'InProgress' },
    { value: StatusCode.Closed, viewValue: 'Closed' },
  ];
  priorityCode: any[] = [
    { value: Priority.Urgent, viewValue: 'Urgent' },
    { value: Priority.High, viewValue: 'High' },
    { value: Priority.Medium, viewValue: 'Medium' },
    { value: Priority.Normal, viewValue: 'Normal' },
    { value: Priority.Low, viewValue: 'Low' },
  ];

  ngOnInit(): void {
    this.fetchUserData();
    this.getCurrentUser();
    this.initForm();
    this.user = JSON.parse(localStorage.getItem('user'));
  }

  getCurrentUser() {
    return this.authenticationService.currentUser
      .pipe(catchError((err) => of(err)))
      .subscribe((user) => (this.user = user));
  }

  fetchUserData() {
    this.userService
      .getAllUser()
      .pipe(catchError((err) => of(err)))
      .subscribe((response) => {
        this.users = response;
      });
  }

  initForm() {
    this.modalForm = this.fb.group({
      id: [null],
      projectName: [null, Validators.required],
      projectType: [null, Validators.required],
      projectCode: [null, Validators.required],
      deadlineDate: [null, Validators.required],
      priorityCode: [Priority.Medium, Validators.required],
      statusCode: [StatusCode.Open],
      departmentId: [null, Validators.required],
      appUserId: [null, Validators.required],
      createDate: [null, Validators.required],
      completeDate: [null],
      description: [null],
      permissionCode: [null],
    });
  }

  openModal(data: any, mode: string, isEdit: boolean) {
    this.isEdit = isEdit;
    this.mode = mode;
    this.data = data;
    this.modalForm.reset();
    if (mode === 'create') {
      this.title = 'New Project';
      this.modalForm.get('priorityCode')?.setValue(Priority.Medium);
      this.modalForm.get('statusCode')?.setValue(StatusCode.Open);
      this.modalForm.get('createDate')?.setValue(new Date());
    } else {
      this.modalForm.patchValue(data);
      this.checkEditForm();
    }
  }

  checkEditForm() {
    this.modalForm.patchValue(this.data);
    if (this.isEdit) {
      this.modalForm.enable();
      this.title = 'Update: ' + this.data.projectName;
    } else {
      this.modalForm.disable();
      this.title = 'View: ' + this.data.projectName;
    }
  }

  submitForm() {
    for (const i in this.modalForm.controls) {
      this.modalForm.controls[i].markAsDirty();
      this.modalForm.controls[i].updateValueAndValidity();
    }
    if (this.modalForm.valid) {
      if (this.mode === 'create') {
        this.projectService
          .createProject(this.modalForm.value)
          .pipe(
            catchError((err) => {
              return of(err);
            })
          )
          .subscribe((response) => {
            if (response) {
              this.toastr.success('Successfully!');
              this.onChangeProject.emit();
            } else {
              this.toastr.error('Failed');
            }
          });
      } else {
        this.modalForm.value.permissionCode = this.user.permissionCode;
        this.projectService
          .updateProject(this.modalForm.value)
          .pipe(
            catchError((err) => {
              this.toastr.error('You not permission');
              return of(err);
            })
          )
          .subscribe((response) => {
            if (!response) {
              this.toastr.success('Successfully!');
              this.onChangeProject.emit();
            }
          });
      }
    }
  }

  onChangeDepartment() {
    const department = this.departments.find(
      (department) => department.id === this.modalForm.value.departmentId
    );
    const user = this.users.find(
      (user) => user.departmentId === department?.id
    );
    this.modalForm.get('appUserId')?.setValue(user?.id);
  }

  onChangeEdit(ev: any) {
    this.isEdit = ev;
    if (Number(this.user.permissionCode) == Permission.Employee) {
      this.isEdit = false;
      this.toastr.warning('You must had permission');
    }
    if (this.mode === 'detail') {
      this.checkEditForm();
    }
  }
}
