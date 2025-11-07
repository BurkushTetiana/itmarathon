terraform {
  backend "s3" {
    bucket       = "terraform-tfstate-firstfxbaget"
    key          = "terraform.tfstate"
    region       = "eu-central-1"
    use_lockfile = true
    encrypt      = true
  }
}
