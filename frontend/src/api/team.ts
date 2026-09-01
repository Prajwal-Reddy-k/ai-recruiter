import apiClient from "./client";
import type { CompanyRoleValue, InterviewAssignment, JobAssignment, TeamMember } from "../types";

export const companyRoleToNumber: Record<CompanyRoleValue, number> = {
  Owner: 1,
  Recruiter: 2,
  HiringManager: 3,
  Interviewer: 4,
};

export async function getMyTeam(): Promise<TeamMember[]> {
  const { data } = await apiClient.get<TeamMember[]>("/company/team");
  return data;
}

export async function addTeamMember(email: string, role: CompanyRoleValue): Promise<TeamMember> {
  const { data } = await apiClient.post<TeamMember>("/company/team/members", { email, role: companyRoleToNumber[role] });
  return data;
}

export async function updateTeamMemberRole(recruiterProfileId: number, role: CompanyRoleValue): Promise<TeamMember> {
  const { data } = await apiClient.put<TeamMember>(`/company/team/members/${recruiterProfileId}/role`, { role: companyRoleToNumber[role] });
  return data;
}

export async function removeTeamMember(recruiterProfileId: number): Promise<void> {
  await apiClient.delete(`/company/team/members/${recruiterProfileId}`);
}

export async function getJobAssignments(jobId: number): Promise<JobAssignment[]> {
  const { data } = await apiClient.get<JobAssignment[]>(`/company/team/jobs/${jobId}/assignments`);
  return data;
}

export async function assignToJob(jobId: number, recruiterProfileId: number): Promise<void> {
  await apiClient.post(`/company/team/jobs/${jobId}/assignments/${recruiterProfileId}`);
}

export async function unassignFromJob(jobId: number, recruiterProfileId: number): Promise<void> {
  await apiClient.delete(`/company/team/jobs/${jobId}/assignments/${recruiterProfileId}`);
}

export async function getInterviewAssignments(interviewId: number): Promise<InterviewAssignment[]> {
  const { data } = await apiClient.get<InterviewAssignment[]>(`/company/team/interviews/${interviewId}/assignments`);
  return data;
}

export async function assignToInterview(interviewId: number, recruiterProfileId: number): Promise<void> {
  await apiClient.post(`/company/team/interviews/${interviewId}/assignments/${recruiterProfileId}`);
}

export async function unassignFromInterview(interviewId: number, recruiterProfileId: number): Promise<void> {
  await apiClient.delete(`/company/team/interviews/${interviewId}/assignments/${recruiterProfileId}`);
}
