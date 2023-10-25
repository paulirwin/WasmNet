;; invoke: br-if
;; expect: (i32:42)

(module
    (func (export "br-if") (result i32) 
        block $b
            i32.const 0
            br_if $b
            i32.const 42
            return
        end
        i32.const 0
    )
)