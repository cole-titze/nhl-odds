/**
 * A team's full name: "Toronto Maple Leafs". The location is left off when the name
 * already starts with it (2024-25's "Utah Hockey Club", location "Utah").
 */
export function fullTeamName(team: { locationName: string; teamName: string }): string {
  const { locationName, teamName } = team;
  return !locationName || teamName.startsWith(`${locationName} `) || teamName === locationName
    ? teamName
    : `${locationName} ${teamName}`;
}
