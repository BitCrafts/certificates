// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using System.ComponentModel.DataAnnotations;

namespace BitCrafts.Certificates.Presentation.ViewModels;

public class CreateServerViewModel
{
    [Required]
    [Display(Name = "Server FQDN")]
    [RegularExpression("^[A-Za-z0-9.-]+$", ErrorMessage = "FQDN contains invalid characters.")]
    public string Fqdn { get; set; } = string.Empty;

    [Display(Name = "IP addresses (optional, comma or space separated)")]
    public string? IpAddresses { get; set; }
}

public class CreateClientViewModel
{
    [Required]
    [Display(Name = "Username")]
    [RegularExpression("^[A-Za-z0-9._-]+$", ErrorMessage = "Username contains invalid characters.")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Email (optional, will be added to SAN)")]
    [EmailAddress]
    public string? Email { get; set; }
}

public class SetupViewModel
{
    [Required]
    [Display(Name = "Intranet base domain (e.g., home.lan)")]
    [RegularExpression("^[a-zA-Z0-9.-]+$", ErrorMessage = "Domain contains invalid characters.")]
    public string Domain { get; set; } = string.Empty;
}
