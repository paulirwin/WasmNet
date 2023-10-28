;; invoke: early_exit
;; expect: (i32:21)

;; source: https://musteresel.github.io/posts/2020/01/webassembly-text-br_table-example.html
(module
    (func (export "early_exit") (result i32)
      (i32.const 21) ;; push some constant value to the stack, to be
                     ;; returned from the function
      (i32.const 0) ;; value used to select a branch
      (br_table 0) ;; table branch with only a default branch
                   ;; default -> (br 0) ;; exits the function
      ;; code below here is never executed!
      (drop) 
      (i32.const 42)
    )
)