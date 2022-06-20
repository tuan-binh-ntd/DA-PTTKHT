﻿using API.Data;
using System;
using API.DTO;
using API.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using API.Enum;
using System.Linq;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using API.DTO.UserDto;

namespace API.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class AccountController : ControllerBase
    {
        private readonly DataContext _dataContext;
        private readonly ITokenService _tokenService;

        public AccountController(DataContext dataContext, ITokenService tokenService)
        {
            _dataContext = dataContext;
            _tokenService = tokenService;
        }

        [HttpGet("getall")]
        [AllowAnonymous]
        public async Task<ActionResult> GetAllUser(Guid? projectId, Guid? departmentId)
        {
            if (projectId != null)
            {
                var userList = await (from p in _dataContext.Project
                                      join t in _dataContext.Task on p.Id equals t.ProjectId
                                      join u in _dataContext.AppUser on t.AppUserId equals u.Id
                                      where p.Id == projectId && u.PermissionCode != Permission.ProjectManager
                                      select new
                                      {
                                          ProjectName = p.ProjectName,
                                          TaskName = t.TaskName,
                                          AppUserId = u.Id,
                                          FirstName = u.FirstName,
                                          LastName = u.LastName,
                                          Address = u.Address,
                                          Email = u.Email,
                                          Password = u.Password,
                                          Phone = u.Phone,
                                          PermissionCode = u.PermissionCode,
                                          DepartmentId = p.DepartmentId,
                                      }).AsNoTracking().ToListAsync();
                /*var groupUserList = userList.GroupBy(e => e.AppUserId).Select(e => new { AppUserId = e.Key, Count = e.Count()}).ToList();
                var result = await (from p in _dataContext.Project
                                    join t in _dataContext.Task on p.Id equals t.ProjectId
                                    join u in _dataContext.AppUser on t.AppUserId equals u.Id
                                    from g in groupUserList 
                                    select new
                                    {
                                        ProjectName = p.ProjectName,
                                        TaskName = t.TaskName,
                                        AppUserId = u.Id,
                                        UserName = u.FirstName + " " + u.LastName,
                                        DepartmentId = p.DepartmentId,
                                        Permission = u.PermissionCode == Permission.Leader ? "Leader" : "Employee"
                                    }).AsNoTracking().ToListAsync();*/
                return Ok(userList);
            }
            if (departmentId != null)
            {
                var userList = await (from d in _dataContext.Department
                                      join u in _dataContext.AppUser on d.Id equals u.DepartmentId
                                      where d.Id == departmentId && u.PermissionCode != Permission.ProjectManager
                                      select new
                                      {
                                          DepartmentId = d.Id,
                                          DepartmentName = d.DepartmentName,
                                          AppUserId = u.Id,
                                          UFirstName = u.FirstName,
                                          LastName = u.LastName,
                                          Address = u.Address,
                                          Password = u.Password,
                                          Email = u.Email,
                                          Phone = u.Phone,
                                          PermissionCode = u.PermissionCode
                                      }).AsNoTracking().ToListAsync();
                //userList.GroupBy(e => e.AppUserId);
                return Ok(userList);
            }
            if (projectId == null && departmentId == null)
            {
                var appUserList = await (from d in _dataContext.Department
                                         join u in _dataContext.AppUser on d.Id equals u.DepartmentId
                                         select new
                                         {
                                             DepartmentId = d.Id,
                                             DepartmentName = d.DepartmentName,
                                             AppUserId = u.Id,
                                             FirstName = u.FirstName,
                                             LastName = u.LastName,
                                             Address = u.Address,
                                             Email = u.Email,
                                             Password = u.Password,
                                             Phone = u.Phone,
                                             PermissionCode = u.PermissionCode
                                         }).AsNoTracking().ToListAsync();
                return Ok(appUserList);
            }
            return Ok();
        }

        [HttpPost("register")]
        public async Task<ActionResult<AppUserDto>> Register(RegisterDto input)
        {
            var newUser = await _dataContext.AppUser.AsNoTracking().FirstOrDefaultAsync(e => e.Email == input.Email);
            if (newUser != null) return BadRequest("Username is taken");
            var leaderExisted = await _dataContext.AppUser.AsNoTracking().FirstOrDefaultAsync(e => e.DepartmentId == input.DepartmentId && e.PermissionCode == Permission.Leader);
            if (leaderExisted != null && input.PermissionCode == Permission.Leader) return BadRequest("Department had leader");
            if (input.PermissionCreator == Permission.Leader && input.PermissionCode == Permission.Employee)
            {
                var user = new AppUser
                {
                    Id = new Guid(),
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    Address = input.Address,
                    Email = input.Email,
                    Phone = input.Phone,
                    Password = input.Password,
                    DepartmentId = input.DepartmentId,
                    PermissionCode = input.PermissionCode
                };
                await _dataContext.AppUser.AddAsync(user);
                await _dataContext.SaveChangesAsync();
                return new AppUserDto
                {
                    Id = user.Id,
                    UserName = user.FirstName + " " + user.LastName,
                    PermissionCode = user.PermissionCode,
                    Email = user.Email,
                    DepartmentId = user.DepartmentId,
                    Token = _tokenService.CreateToken(user)
                };
            }
            else if (input.PermissionCreator == Permission.ProjectManager && (input.PermissionCode == Permission.Employee || input.PermissionCode == Permission.Leader))
            {
                var user = new AppUser
                {
                    Id = new Guid(),
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    Address = input.Address,
                    Email = input.Email,
                    Phone = input.Phone,
                    Password = input.Password,
                    DepartmentId = input.DepartmentId,
                    PermissionCode = input.PermissionCode
                };
                await _dataContext.AppUser.AddAsync(user);
                await _dataContext.SaveChangesAsync();
                return new AppUserDto
                {
                    Id = user.Id,
                    UserName = user.FirstName + " " + user.LastName,
                    PermissionCode = user.PermissionCode,
                    Email = user.Email,
                    DepartmentId = user.DepartmentId,
                    Token = _tokenService.CreateToken(user)
                };
            }
            else
            {
                return Unauthorized("You must had permission");
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AppUserDto>> Login(LoginDto input)
        {
            var user = await _dataContext.AppUser.AsNoTracking().FirstOrDefaultAsync(e => e.Email == input.Email);
            if (user == null) return Unauthorized("Invalid username");
            var pass = await _dataContext.AppUser.AsNoTracking().FirstOrDefaultAsync(e => e.Email == input.Email && e.Password == input.Password);
            if (pass == null) return Unauthorized("Invalid password");
            return new AppUserDto
            {
                Id = user.Id,
                UserName = user.FirstName + " " + user.LastName,
                PermissionCode = user.PermissionCode,
                Email = user.Email,
                DepartmentId = user.DepartmentId == null ? null : user.DepartmentId,
                Token = _tokenService.CreateToken(user)
            };
        }

        [HttpPut("changepassword")]
        public async Task<ActionResult> ChangePassword(ChangePasswordDto input)
        {
            var user = await _dataContext.AppUser.SingleOrDefaultAsync(e => e.Email == input.Email);
            user.Password = input.Password;
            _dataContext.AppUser.Update(user);
            await _dataContext.SaveChangesAsync();
            return Ok(user);
        }

        [HttpPut("update")]
        public async Task<ActionResult> UpdateUser(UpdateUserDto input)
        {
            var user = await _dataContext.AppUser.FindAsync(input.Id);
            var leader = await _dataContext.AppUser.FirstOrDefaultAsync(e => e.DepartmentId == input.DepartmentId);
            if(input.PermissionCode == Permission.ProjectManager)
            {
                return BadRequest("Company had project manager");
            }
            if (leader != null && input.PermissionCode == Permission.Leader)
            {
                return BadRequest("Department had leader");
            }
            if (user != null)
            {
                if (input.PermissionCode == Permission.Employee)
                {
                    user.FirstName = user.FirstName;
                    user.LastName = user.LastName;
                    user.Address = user.Address;
                    user.Email = user.Email;
                    user.Phone = user.Phone;
                    user.Password = input.Password;
                    user.DepartmentId = user.DepartmentId;
                    _dataContext.AppUser.Update(user);
                    await _dataContext.SaveChangesAsync();
                    return Ok(user);
                }
                else
                {
                    user.FirstName = input.FirstName;
                    user.LastName = input.LastName;
                    user.Address = input.Address;
                    user.Email = input.Email;
                    user.Phone = input.Phone;
                    user.Password = input.Password;
                    user.DepartmentId = input.DepartmentId;
                    _dataContext.AppUser.Update(user);
                    await _dataContext.SaveChangesAsync();
                    return Ok(user);
                }

            }
            else
            {
                return BadRequest("User not existed");
            }
        }

        [HttpDelete("delete")]
        public async Task<ActionResult> DeteleUser(Guid deleteUserId, Guid deletedUserId, Permission deleteUserPermission, 
            Permission deletedUserPermission, Guid newLeaderId)
        {
            if(deleteUserPermission == Permission.ProjectManager && deletedUserPermission == Permission.Leader)
            {
                // Lấy Leader mới, gán lại quyền và lưu lại
                var newLeader = await _dataContext.AppUser.FindAsync(newLeaderId);
                newLeader.PermissionCode = Permission.Leader;
                _dataContext.AppUser.Update(newLeader);
                await _dataContext.SaveChangesAsync();
                // Lấy danh sách các công việc của Leader chuẩn bị xóa và lưu
                var taskOldLeader = await _dataContext.Task.Where(e => e.CreateUserId == deletedUserId || e.AppUserId == deletedUserId).ToListAsync();
                foreach(var item in taskOldLeader)
                {
                    //Các công việc của Leader thì gán AppUserId cho leader mới
                    if(item.AppUserId == deletedUserId)
                    {
                        item.AppUserId = newLeader.Id;
                    }
                    item.CreateUserId = newLeader.Id;
                }
                _dataContext.Task.UpdateRange(taskOldLeader);
                await _dataContext.SaveChangesAsync();
                //Xóa leader
                _dataContext.AppUser.Remove(await _dataContext.AppUser.FindAsync(deletedUserId));
                await _dataContext.SaveChangesAsync();
                return Ok("Removed");
            }
            else if(deleteUserPermission == Permission.ProjectManager && deletedUserPermission == Permission.Employee ||
                deleteUserPermission == Permission.Leader && deletedUserPermission == Permission.Employee)
            {
                //Lấy danh sách công việc của employee
                var taskOldEmployee = await _dataContext.Task.Where(e => e.AppUserId == deletedUserId).ToListAsync();
                //Lấy leader của phòng có employee
                var leader = await _dataContext.AppUser.FindAsync(deleteUserId);
                //Gán công việc cho leader và lưu
                foreach(var item in taskOldEmployee)
                {
                    item.AppUserId = leader.Id;
                }
                _dataContext.Task.UpdateRange(taskOldEmployee);
                _dataContext.SaveChanges();
                //Xóa employee
                _dataContext.AppUser.Remove(await _dataContext.AppUser.FindAsync(deletedUserId));
                _dataContext.SaveChanges();
                return Ok("Removed");
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
