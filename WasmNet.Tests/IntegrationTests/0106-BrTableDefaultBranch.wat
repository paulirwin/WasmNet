;; invoke: nop
;; NOTE: this just ensures that the br_table instruction doesn't break with a single default branch

;; source: https://musteresel.github.io/posts/2020/01/webassembly-text-br_table-example.html
(module
    (func (export "nop")
        (i32.const 1)    ;; value used to select a branch
        (br_table 0)     ;; table branch with only a default branch
    ) 
)