terraform {
  backend "s3" {
    bucket       = "firstfxbaget"
    key          = "terraform.tfstate"
    region       = "eu-central-1"
    
    encrypt      = true
  }
}
