import { Component, OnInit, ViewChild } from '@angular/core';
import { catchError, of } from 'rxjs';
import { ProjectService } from '../../services/project.service';
import { ModalProjectComponent } from '../shared/modal-project/modal-project.component';
import * as bootstrap from 'bootstrap';
import { Router } from '@angular/router';
import { Permission } from 'src/app/helpers/PermisionEnum';
import { ToastrService } from 'ngx-toastr';
import { DepartmentService } from 'src/app/services/department.service';
import { GetAllProject } from 'src/app/models/getallproject';
import { User } from 'src/app/models/user';
@Component({
  selector: 'app-projects',
  templateUrl: './projects.component.html',
  styleUrls: ['./projects.component.css'],
})
export class ProjectsComponent implements OnInit {
  constructor(
    private projectService: ProjectService,
    private departmentService: DepartmentService,
    private router: Router,
    private toastr: ToastrService
  ) {}
  @ViewChild('modalProject') modalProject!: ModalProjectComponent;
  $: any;
  data: any[] = [];
  departments: any[] = [];
  allRecord: number = 0;
  resolvedRecord: number = 0;
  inProgressRecord: number = 0;
  closedRecord: number = 0;
  isAllRecord: boolean = true;
  isResolvedRecord: boolean = false;
  isInProgressRecord: boolean = false;
  isClosedRecord: boolean = false;
  isShowModal: boolean = false;
  right: boolean = false;
  user: User;
  getAllProject: GetAllProject = new GetAllProject();
  ngOnInit(): void {
    const user = JSON.parse(localStorage.getItem('user'))
    this.user = JSON.parse(localStorage.getItem('user'))
    if(user.permissionCode === Permission.ProjectManager || user.permissionCode === Permission.Leader){
      this.right = true
    }
    this.fetchDepartmentData();
    this.fetchProjectData();
  }

  fetchDepartmentData() {
    this.departmentService
      .getAllDepartment()
      .pipe(catchError((err) => of(err)))
      .subscribe((response) => {
        this.departments = response;
      });
  }

  fetchProjectData() {
    this.getAllProject.departmentId = this.user.departmentId
    this.projectService
      .getAllProject(this.getAllProject)
      .pipe(catchError((err) => of(err)))
      .subscribe((response) => {
        this.data = response;
        this.allRecord = this.data.length;
      });
  }

  getDepartmentName(id: string) {
    return this.departments.find((department) => department.id === id)
      ?.departmentName;
  }

  openDetailModal(data: any, mode: string, isEdit: boolean) {
    var myModal = new bootstrap.Modal(
      document.getElementById('createProjectModal')!
    );
    myModal.show();
    this.isShowModal = true;
    this.modalProject.openModal(data, mode, isEdit);
  }

  onViewTask(projectId: string): any {
    this.router.navigate(['projects/tasks', { projectId }]);
  }

  onChangeProject() {
    var myModal = new bootstrap.Modal(
      document.getElementById('createProjectModal')!
    );

    // $('#createProjectModal').modal('hide')
    // $('#createProjectModal').hide;
    myModal.hide();
    $(document.body).removeClass('modal-open');
    $('.modal-backdrop').remove();
      this.isShowModal = false;
    this.fetchProjectData();
  }

  roundProgress(progress: number){
    return Math.round(progress) + '%'
  }

  openModal(){
      this.modalProject.openModal(null, 'create', true);
      this.isShowModal = true;
  }

  onSearch(ev:any){
    if (ev.key === "Enter") {
      debugger
      this.getAllProject.keyWord = ev.target.value;
      this.fetchProjectData();
    }
  }

}
